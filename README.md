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

```sh
git clone --depth 1 https://github.com/TimmyWong-TW/Maize-KB.git
cd Maize-KB/tools
docker compose build
```

1. 大量下載音頻及字幕。  
   Bulk download audios and captions.
   ```sh
   docker compose run --rm vimeo-downloader
   ```
1. 若來源缺乏字幕，辨識漢語以生成中文文本並對齊音頻時間。  
   When source lacks captions, transcribe Mandarin to Chinese and align with audio.
   ```sh
   docker compose run --rm transcriber
   docker compose run --rm whisperx-json-trimmer
   ```
1. 將文本轉換成臺灣中文。  
   Convert transcripts into Chinese (Taiwan).
   ```sh
   docker compose run --rm chinese-converter
   ```
1. 使用字幕編輯工具校對文本以便大量更正。  
   Proofread transcripts with a subtitle editor to identify misrecognitions for batch correction.
1. 需要時，以語音停頓時間分段，然後統一標點符號，繼而重新對齊音頻時間用以供字幕使用。  
   Optionally, arrange into paragraphs by pauses in speech, and then unify punctuations, before re-alignment of clauses for captioning.
1. 分門別類，標註講員。  
   Classify, and diarize speakers.
