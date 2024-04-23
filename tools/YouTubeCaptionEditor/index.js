// e.g. #https://raw.githubusercontent.com/TimmyWong-TW/Maize-KB/main/whisperx/youtube/%E6%B7%A1%E6%B1%9F%E6%95%99%E6%9C%83-%E5%9C%8B%E5%BA%A6%E5%BD%B1%E9%9F%B3/%E4%BB%A5%E5%BC%97%E6%89%80%E6%9B%B8/w29_YOajQ1c%3D170914%20%E4%BB%A5%E5%BC%97%E6%89%80%E6%9B%B8%201%E7%AB%A01%7E14%E7%AF%80.json
(async () => {
    const youtubeId = location.hash?.match(/(?<=\/youtube\/.+\/)[^/=]+(?=(=|%3D)[^\/]+$)/)?.[0];
    if (youtubeId) {
        const [{ segments }, player] = await Promise.all([
            fetch(location.hash.substring(1)).then(response => response.json()),
            fetch(`https://youtube.timtim.workers.dev/?v=${youtubeId}`)
                .then(response => response.json())
                .then(adaptiveFormats => adaptiveFormats
                    .filter(f => f?.mimeType.match(/^audio\//) && MediaSource.isTypeSupported(f.mimeType))
                    .map(f => {
                        const a = f.mimeType.substring(6, f.mimeType.indexOf(';', 7));
                        return {
                            f, q: {
                                AUDIO_QUALITY_LOW: 0,
                                AUDIO_QUALITY_MEDIUM: 1,
                                AUDIO_QUALITY_HIGH: 2
                            }[f.audioQuality], i: {
                                mp4: 0,
                                webm: 1
                            }[a] || -1
                        };
                    }).reduce((max, current) =>
                        current.q > max.q ||
                            current.q == max.q && (
                                current.i > max.i ||
                                current.i == max.i && current.f.bitrate > max.f.bitrate
                            ) ? current : max,
                        { q: -1, i: -2 }
                    ).f.url)
                .then(url => new Promise((resolve, reject) => {
                    const audio = new Audio(url); // TODO: buffer ranges with Media Source Extensions to bypass throttling of progressive download
                    audio.controls = true;
                    audio.addEventListener("canplaythrough", e => resolve(e.target));
                    audio.addEventListener("error", e => reject(e.target.error));
                }))
        ]), transcript = segments.map(({ text, start, end, words }) => {
            const line = document.createElement("div");
            if (start || end) {
                line.segment = { start, end };
            }
            words.map(({ word, start, end, score }) => {
                const span = document.createElement("span");
                span.textContent = word;
                if (start || end) {
                    span.segment = { start, end };
                }
                line.appendChild(span);
                return span;
            });
            return line;
        }).reduce((t, line) => {
            t.appendChild(line);
            return t;
        }, document.createElement("div")), main = document.querySelector("main");
        transcript.addEventListener("click", e => {
            if (!getSelection()?.toString()) {
                let span = e.target;
                while (span && !span.segment) {
                    span = span.nextElementSibling || span.parentElement?.nextElementSibling?.firstElementChild;
                }
                if (span?.segment) {
                    player.currentTime = span.segment.start;
                }
            }
        });
        transcript.addEventListener("dblclick", e => player.play());
        let updateTimeout;
        player.addEventListener("timeupdate", function update(e) {
            clearTimeout(updateTimeout);
            updateTimeout = undefined;
            const t = e.target.currentTime;
            for (const line of transcript.children) {
                line.classList.toggle("current", line.segment && line.segment.start <= t && line.segment.end > t);
                for (const span of line.children) {
                    if (span.segment && span.segment.start <= t && span.segment.end > t) {
                        span.classList.add("current");
                        const { top, bottom } = span.getBoundingClientRect();
                        if (top < transcript.parentElement.getBoundingClientRect().top || bottom > player.getBoundingClientRect().top) {
                            span.scrollIntoView({
                                behavior: "instant",
                                block: "center"
                            });
                        }
                        if (!player.paused) {
                            updateTimeout = setTimeout(update.bind(this, e), (span.segment.end - t) * 1000);
                        }
                    } else {
                        span.classList.remove("current");
                    }
                }
            }
        });
        main.appendChild(transcript);
        main.appendChild(player);
    }
})().catch(console.error);