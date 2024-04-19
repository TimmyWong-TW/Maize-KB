# 🌽小玉米知識庫 Maize Knowledge Base

## 資料來源 Data Source

- Vimeo
  - [淡江教會](https://vimeo.com/user2178983)
    - 1,242 段音頻，共 47.5 GB
    - 檔案名稱以影片識別碼為首，以等號分隔標題，遇到路徑符號皆改為全形。  
      File names begin with video ID followed by the title after an equal sign, with path characters converted to full-width.
- YouTube
  - [淡江教會-國度影音](https://www.youtube.com/channel/UCTvOJF2jWzrSOCXuXI-pgNQ)
    - [聖經師資初階培訓](https://www.youtube.com/playlist?list=PLe-YK1dmFUsLnnUV54cwFLCc8nXP2TUvz)
    - [以弗所書](https://www.youtube.com/playlist?list=PLe-YK1dmFUsL0j_THqhwbZFpj0BuXdpJj)
    - ……
  - [淡江教會淡水堂](https://www.youtube.com/channel/UCx6fUQUflVPgUbkY7GWJMwg)
    - [2024 主日崇拜](https://www.youtube.com/playlist?list=PLV4YZBS1Bq3dQXmB8dx0Ex6w6dMfs0SW4)
    - [2023 主日崇拜](https://www.youtube.com/playlist?list=PLV4YZBS1Bq3dmq9ApN3-b2uTICgKJCnVj)
    - [2022 主日崇拜](https://www.youtube.com/playlist?list=PLV4YZBS1Bq3dMGy_e4c-H0K0M9n9uCl3e)
    - [2021 主日崇拜](https://www.youtube.com/playlist?list=PLV4YZBS1Bq3ep6HtFzrNjmFPupiZbnL_P)
    - [2020 主日崇拜](https://www.youtube.com/playlist?list=PLV4YZBS1Bq3eJ7LOUUEh_0dAXnKePUWd2)
  - [淡江教會淡海堂](https://www.youtube.com/channel/UC-6ac1QQifgsvXhFpL_wnZw)
    - [2024 主日崇拜](https://www.youtube.com/playlist?list=PLyCjLfWbz6idH6SzBO02YED9Bb-3GqnWp)
    - [2023 主日崇拜](https://www.youtube.com/playlist?list=PLyCjLfWbz6iehtHyI_DT_EVeatcwF4YgL)
    - [2022 主日崇拜](https://www.youtube.com/playlist?list=PLyCjLfWbz6icBuJAbYAlPwl6pl6TKPFK5)
    - [2021 主日崇拜](https://www.youtube.com/playlist?list=PLyCjLfWbz6ifHFuXI4BNkboMVpkcR_q-T)
  - [淡江教會桃園堂](https://www.youtube.com/channel/UCcdIbQvRl8i6tEuKiYQgMAw)
    - [2024 主日崇拜](https://www.youtube.com/playlist?list=PL9BI9uMgbGFdUu4r8tU3P04LGANbXjGaF)
    - [2023 主日崇拜](https://www.youtube.com/playlist?list=PL9BI9uMgbGFcWX50Cr_CAysMp4NECsa6j)
    - [2022 主日崇拜](https://www.youtube.com/playlist?list=PL9BI9uMgbGFdpwyDZx3nxSRcCDsEQQGCY)
  - [淡江教會高雄堂](https://www.youtube.com/channel/UCXLpnJfevlM4Y57jIiQRuXg)
    - [2024 主日崇拜](https://www.youtube.com/playlist?list=PLLIqo6dtvjwOx0QjCZWfaZsmphSHLNRtS)

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
   docker compose run --rm -d -w /data/source/vimeo/淡江教會 -e USER_ID=2178983 vimeo-downloader
   ```
   ```sh
   docker compose run --rm -d -e PLAYLIST_ID=PLe-YK1dmFUsLnnUV54cwFLCc8nXP2TUvz youtube-dl
   docker compose run --rm -d -e PLAYLIST_ID=PLe-YK1dmFUsL0j_THqhwbZFpj0BuXdpJj youtube-dl
   # ……
   docker compose run --rm -d -e PLAYLIST_ID=PLV4YZBS1Bq3dQXmB8dx0Ex6w6dMfs0SW4 youtube-dl
   docker compose run --rm -d -e PLAYLIST_ID=PLV4YZBS1Bq3dmq9ApN3-b2uTICgKJCnVj youtube-dl
   docker compose run --rm -d -e PLAYLIST_ID=PLV4YZBS1Bq3dMGy_e4c-H0K0M9n9uCl3e youtube-dl
   docker compose run --rm -d -e PLAYLIST_ID=PLV4YZBS1Bq3ep6HtFzrNjmFPupiZbnL_P youtube-dl
   docker compose run --rm -d -e PLAYLIST_ID=PLV4YZBS1Bq3eJ7LOUUEh_0dAXnKePUWd2 youtube-dl
   ```
   ……
1. 若來源缺乏字幕，辨識漢語以生成中文文本並對齊音頻時間。  
   When source lacks captions, transcribe Mandarin to Chinese and align with audio.  
   如預算許可則考慮使用更佳模型。  
   Consider Gemini 1.5 Pro with audio input over Whisper Large v3 when budget allows.
   ```sh
   docker compose run --rm -d transcriber
   docker compose run --rm whisperx-json-trimmer
   docker compose run --rm resegmenter
   ```
1. 將文本轉換成臺灣中文，然後自行校對。  
   Convert transcripts into Chinese (Taiwan), and then proofread manually.
   ```sh
   docker compose run --rm chinese-converter
   ```
1. 使用字幕編輯工具校對文本以便大量更正。  
   Proofread transcripts with a subtitle editor to identify misrecognitions for batch correction.
1. 需要時，以語音停頓時間分段，然後統一標點符號，繼而重新對齊音頻時間用以供字幕使用。  
   Optionally, arrange into paragraphs by pauses in speech, and then unify punctuations, before re-alignment of clauses for captioning.
1. 分門別類，標註講員。  
   Classify, and diarize speakers.