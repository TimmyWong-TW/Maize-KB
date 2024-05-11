import { promises as fs } from 'fs';
import { traditionalize } from 'hanzi-tools';
import { glob } from 'glob';
await Promise.all(glob.sync('/data/tsv/**/*.tsv').map(async tsv => {
    const tmp = tsv + '~', lines = (await fs.readFile(tsv, 'utf-8')).split('\n');
    await fs.writeFile(tmp, [
        lines[0],
        ...lines.slice(1).map(l => {
            const [start, end, text] = l.split('\t');
            return `${start}\t${end}\t${traditionalize(text)
                .replace(/(禰)|(麵對)|(裡路)|((?<=不單?)隻)|((?<=[水泳].*)遊)|((?<=只)準|(?<![水不看瞄對標])準(?=[他了許我你她祂牠它]))|(面嚮)/g, (_, a, b, c, d, e, f, g) =>
                    a ? '祢' : b ? '面對' : c ? '里路' : d ? '只' : e ? '游' : f ? '准' : g ? '面向' : _)}`;
        })
    ].join('\n'));
    try {
        await fs.unlink(tsv);
    } catch { }
    await fs.rename(tmp, tsv);
}));