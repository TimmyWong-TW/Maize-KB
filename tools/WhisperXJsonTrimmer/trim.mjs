import { promises as fs } from 'fs';
import { glob } from 'glob';
for (const json of glob.sync('/data/whisperx/**/*.json')) {
    const { word_segments, ...rest } = JSON.parse(await fs.readFile(json, 'utf-8'));
    await fs.writeFile(json, JSON.stringify(rest));
}