# 🌽小玉米知識庫 Maize Knowledge Base

## 資料來源 Data Source

- Vimeo
  - [淡江教會](https://vimeo.com/user2178983)
    - 1,242 段音頻，共 47.5 GB
    - 檔案名稱以影片識別碼為首，以等號分隔標題，遇到路徑符號皆改為全形。  
      File names begin with video ID followed by the title after an equal sign, with path characters converted to full-width.
- YouTube
  - [淡江教會-國度影音](https://www.youtube.com/channel/UCTvOJF2jWzrSOCXuXI-pgNQ)
  - [淡江教會淡水堂](https://www.youtube.com/channel/UCx6fUQUflVPgUbkY7GWJMwg)
  - [淡江教會淡海堂](https://www.youtube.com/channel/UC-6ac1QQifgsvXhFpL_wnZw)
  - [淡江教會桃園堂](https://www.youtube.com/channel/UCcdIbQvRl8i6tEuKiYQgMAw)
  - [淡江教會高雄堂](https://www.youtube.com/channel/UCXLpnJfevlM4Y57jIiQRuXg)

## 擷取方法 Retrieval Methodology
注意：以下筆記僅供參考，並非實際記錄完整步驟。  
Note: incomplete documentation without full procedure for reference only.

1. 使用適當工具大量下載音頻及字幕。若來源同時包含視頻及音頻，可刪除視頻部分以節省儲存空間。  
   Bulk download audios and captions with appropriate tools. In case video is bundled with audio, the video part may be removed to save storage space.
   ```sh
   ffmpeg -i "videos/file.mp4" -vn -c copy "audios/file.m4a" && rm "videos/file.mp4"
   ```
1. 若來源缺乏字幕，辨識漢語以生成中文文本並對齊音頻時間。  
   When source lacks captions, transcribe Mandarin to Chinese and align with audio.
   ```Dockerfile
   FROM pytorch/pytorch:2.2.2-cuda12.1-cudnn8-runtime
   RUN apt-get update && apt-get install -y git && apt-get clean && rm -rf /var/lib/apt/lists/*
   RUN pip install git+https://github.com/m-bain/whisperx.git
   ```
   ```sh
   docker build --tag whisperx .
   docker run --rm -it -v whispers:/root/.cache -v ./:/data --gpus all whisperx
   ```
   ```sh
   whisperx --model large-v3 --output_format json --task transcribe --language zh --align_model StevenLimcorn/wav2vec2-xls-r-300m-zh-TW --initial_prompt "弟兄姊妹平安，我們一起來思想。繁體中文，臺灣國語。" --output_dir /data/transcripts /data/audios/*.m4a
   ```
1. 需要時，以語音停頓時間分段，然後統一標點符號，用以細分時間供字幕使用。  
   Optionally, arrange into paragraphs by pauses in speech, and then unify punctuations, to separate clauses for captioning.
1. 將文本轉換成臺灣中文。  
   Convert transcripts into Chinese (Taiwan).
   ```sh
   npm install opencc
   ```
   ```ts
   import { promises as fs } from 'fs';
   import { OpenCC } from 'opencc';
   async function cc() {
       const converter = new OpenCC('s2tw.json');
       const { segments } = JSON.parse(await fs.readFile('whisperx/file.json', 'utf-8'));
       await fs.writeFile('transcripts/file.tsv', [
           'start\tend\ttext',
           ...Promise.all(segments.map(async ({ start, end, text }) => [
               start,
               end,
               await converter.convertPromise(text)
           ].join('\t')))
       ].join('\n'));
   }
   cc();
   ```
1. 使用字幕編輯工具修正文本、大量更正常見錯別字。  
   Proofread transcripts with a subtitle editor, fix common errors.
1. 分門別類，標註講員。  
   Classify, and diarize speakers.
