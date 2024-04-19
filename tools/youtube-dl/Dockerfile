FROM alpine
ENV PLAYLIST_ID=
RUN apk add --no-cache ffmpeg youtube-dl
CMD youtube-dl -o "/data/source/youtube/%(channel)s/%(playlist_title)s/%(id)s=%(title)s.%(ext)s" -f bestaudio/best -x --write-subs --sub-format srv1/vtt/best --sub-lang zh-TW https://www.youtube.com/playlist?list=${PLAYLIST_ID}