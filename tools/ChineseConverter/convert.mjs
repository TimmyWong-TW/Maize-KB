import { promises as fs } from 'fs';
import OpenCC from 'opencc';
import { glob } from 'glob';
const converter = new OpenCC('s2tw.json');
await Promise.all(glob.sync('/data/tsv/**/*.tsv').map(async tsv => {
    const tmp = tsv + '~';
    await fs.writeFile(tmp, [
        'start\tend\ttext',
        ...await Promise.all((await fs.readFile(tsv, 'utf-8'))
            .split('\n').slice(1)
            .map(async l => {
                const [start, end, text] = l.split('\t');
                return `${start}\t${end}\t${await converter.convertPromise(text)}`;
            }))
    ].join('\n'));
    await fs.unlink(tsv);
    await fs.rename(tmp, tsv);
}));