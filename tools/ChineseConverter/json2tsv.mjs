import { promises as fs } from 'fs';
import OpenCC from 'opencc';
import { glob } from 'glob';
import path from 'path';
const converter = new OpenCC('s2tw.json');
await Promise.all(glob.sync('/data/whisperx/**/*.json').map(async json => {
    const tsv = path.join('/data/tsv', path.relative('/data/whisperx', json).replace(/.json$/, '.tsv')),
        tmp = tsv + '~';
    await fs.mkdir(path.dirname(tsv), { recursive: true });
    if (!fs.access(tsv, fs.constants.W_OK)) {
        const { segments } = JSON.parse(await fs.readFile(json, 'utf-8'));
        await fs.writeFile(tmp, [
            'start\tend\ttext',
            ...await Promise.all(segments.map(async ({ start, end, text }) => [
                start,
                end,
                await converter.convertPromise(text)
            ].join('\t')))
        ].join('\n'));
        await fs.rename(tmp, tsv);
    }
}));