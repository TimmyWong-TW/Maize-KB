import { promises as fs } from 'fs';
import { glob } from 'glob';
import path from 'path';
import { GoogleGenerativeAI, HarmCategory, HarmBlockThreshold } from '@google/generative-ai';

let canceled = false, cancelTimeout = () => { };
function cancel() {
    canceled = true;
    cancelTimeout();
}
process.on('SIGINT', cancel);
process.on('SIGTERM', cancel);

const RPM = 15, requestInterval = 6e4 / RPM, TPM = 32000, timeouts = [];
let lastRequest = Date.now() - requestInterval;
let accumulatedTokens, resetTokens, lastTokenReset;

async function describe(text) {
    const interval = Date.now() - lastRequest;
    if (interval < requestInterval) {
        await new Promise((resolve, reject) => {
            const timeout = setTimeout(resolve, requestInterval - interval);
            cancelTimeout = () => {
                clearTimeout(timeout);
                reject('Canceled');
            };
        });
    }
    if (!resetTokens) {
        resetTokens = setInterval(() => {
            accumulatedTokens = 0;
            lastTokenReset = Date.now();
        }, 6e4);
        accumulatedTokens = 0;
        lastTokenReset = Date.now();
    }
    const genAI = new GoogleGenerativeAI(process.env.GOOGLE_API_KEY);
    const model = genAI.getGenerativeModel({ model: 'gemini-1.0-pro' });
    const generationConfig = {
        temperature: 0,
        topK: 1,
        topP: 1,
        maxOutputTokens: 2048,
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
        await new Promise((resolve, reject) => {
            const timeout = setTimeout(resolve, lastTokenReset + 6e4 - Date.now());
            cancelTimeout = () => {
                clearTimeout(timeout);
                reject('Canceled');
            };
        });
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

const skip = (await fs.readFile('/data/overview/error.log', 'utf-8')).split('\n');
for (const tsv of glob.sync('/data/tsv/**/*.tsv')) {
    if (canceled) {
        console.error("Canceled");
        process.exit(0);
    }
    const md = path.join('/data/overview', path.relative('/data/tsv', tsv).replace(/.tsv$/, '.md'));
    if (skip.find(s => s === tsv || s === md)) {
        continue;
    }
    await fs.mkdir(path.dirname(md), { recursive: true });
    try {
        await fs.access(md, fs.constants.W_OK);
        // skip existing
    } catch (e) {
        if (e.errno === -2) {
            const tmp = md + '~', transcript = (await fs.readFile(tsv, 'utf-8'))
                .split('\n').slice(1)
                .map(l => l.split('\t')[2])
                .join(' ');
            if (transcript.length > 0) {
                let overview;
                try {
                    overview = await describe(transcript);
                } catch (e) {
                    // TODO: fall back to OpenAI GPT?
                    console.error(e);
                    console.error(tsv);
                    await fs.appendFile('/data/overview/error.log', tsv + '\n');
                    continue;
                }
                if (overview?.length > 0) {
                    await fs.writeFile(tmp, overview);
                    await fs.rename(tmp, md);
                } else {
                    console.error(md);
                    await fs.appendFile('/data/overview/error.log', md + '\n');
                    continue;
                }
            }
        } else {
            throw e; // could not determine existence
        }
    }
}
clearInterval(resetTokens);