import { promises as fs } from 'fs';
import { glob } from 'glob';
for (const json of glob.sync('/data/whisperx/**/*.json')) {
    const tmp = json + '~',
        { word_segments, ...rest } = JSON.parse(await fs.readFile(json, 'utf-8'));
    try { await fs.rename(json, tmp); } catch { }
    await fs.writeFile(json, JSON.stringify(rest));
    await fs.unlink(tmp);
}