import { promises as fs } from 'fs';
import { glob } from 'glob';
import path from 'path';
await Promise.all(glob.sync('/kb/tsv/**/*.tsv').map(async tsv => {
    const txt = '/kb/txt/' + tsv.substring(8, tsv.length - 3) + 'txt',
        tmp = txt + '~',
        lines = (await fs.readFile(tsv, 'utf-8')).split('\n');
    await fs.mkdir(path.dirname(txt), { recursive: true });
    await fs.writeFile(tmp, lines.slice(1).map(l => l.split('\t')[2]).join('\n'));
    try {
        await fs.unlink(txt);
    } catch { }
    await fs.rename(tmp, txt);
}));