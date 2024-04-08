import { promises as fs } from 'fs';
import { glob } from 'glob';
import path from 'path';
import { GoogleGenerativeAI, HarmCategory, HarmBlockThreshold } from '@google/generative-ai';

const RPM = 15, requestInterval = 6e4 / RPM, TPM = 32000;
let lastRequest = Date.now() - requestInterval;
let accumulatedTokens = 0, resetTokens, lastTokenReset;

async function describe(text) {
    const interval = Date.now() - lastRequest;
    if (interval < requestInterval) {
        await new Promise(resolve => setTimeout(resolve, requestInterval - interval));
    }
    if (!resetTokens) {
        resetTokens = setInterval(() => {
            accumulatedTokens = 0;
            lastTokenReset = Date.now();
        }, 6e4);
    }
    const genAI = new GoogleGenerativeAI(process.env.GOOGLE_API_KEY);
    const model = genAI.getGenerativeModel({ model: 'gemini-1.0-pro' });
    const generationConfig = {
        temperature: 0,
        topK: 1,
        topP: 1,
        maxOutputTokens: 512,
    }, safetySettings = [
        HarmCategory.HARM_CATEGORY_HARASSMENT,
        HarmCategory.HARM_CATEGORY_HATE_SPEECH,
        HarmCategory.HARM_CATEGORY_SEXUALLY_EXPLICIT,
        HarmCategory.HARM_CATEGORY_DANGEROUS_CONTENT
    ].map(category => ({
        category,
        threshold: HarmBlockThreshold.BLOCK_NONE
    })), prompt = `The text below is transcribed from Taiwan Mandarin, and may contain speech recognition errors due to homophones or otherwise that require correction, especially for names in the Bible. Generate a short title, a summary and an outline in Traditional Chinese after correction. Address the author as "講員" instead of "作者".\n"""\n${text}\n"""`;
    const tokenCount = await model.countTokens(prompt);
    if (accumulatedTokens + tokenCount > TPM) {
        await new Promise(resolve => setTimeout(resolve, lastTokenReset + 6e4));
    }
    const result = await model.generateContent({
        contents: [{ role: 'user', parts: [{ text: prompt },] }],
        generationConfig,
        safetySettings
    });
    lastRequest = Date.now();
    accumulatedTokens += tokenCount;
    return result.response.text();
}

let canceled = false;
process.on('SIGINT', () => canceled = true);
process.on('SIGTERM', () => canceled = true);

for (const tsv of glob.sync('/data/tsv/**/*.tsv')) {
    if (canceled) {
        console.error("Canceled");
        break;
    }
    const md = path.join('/data/overview', path.relative('/data/tsv', tsv).replace(/.tsv$/, '.md'));
    await fs.mkdir(path.dirname(md), { recursive: true });
    try {
        await fs.access(md, fs.constants.W_OK);
        // skip existing
    } catch (e) {
        if (e.errno === -2) {
            const tmp = md + '~';
            await fs.writeFile(tmp, await describe(
                (await fs.readFile(tsv, 'utf-8'))
                    .split('\n')
                    .map(l => l.split('\t')[2])
                    .join('\n')
            ));
            await fs.rename(tmp, md);
            console.log(md);
        } else {
            throw e; // could not determine existence
        }
    }
}