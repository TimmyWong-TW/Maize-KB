// e.g. #https://raw.githubusercontent.com/TimmyWong-TW/Maize-KB/main/whisperx/vimeo/%E6%B7%A1%E6%B1%9F%E6%95%99%E6%9C%83/%E4%B8%BB%E6%97%A5%E4%BF%A1%E6%81%AF/6158626%3D090809%20%E9%A0%86%E6%9C%8D%E7%9A%84%E9%A0%98%E8%A2%96%20%E8%8E%8A%E8%82%B2%E9%8A%98%E7%89%A7%E5%B8%AB%20%EF%BC%8F%20%E6%B0%91%E6%95%B8%E8%A8%98.json
(async () => {
    // function formatTime(seconds) {
    //     const h = Math.floor(seconds / 3600),
    //         m = Math.floor(seconds % 3600 / 60).toString().padStart(2, "0"),
    //         s = Math.floor(seconds % 60).toString().padStart(2, "0"),
    //         z = Math.floor(seconds % 1 * 1000).toString().padStart(3, "0");
    //     return h ? `${h}:${m}:${s}.${z}` : `${m}:${s}.${z}`;
    // }
    const vimeoId = location.hash?.match(/(?<=\/vimeo\/.+\/)\d+(?=(=|%3D)[^\/]+$)/)?.[0];
    if (vimeoId) {
        const [main] = document.getElementsByTagName("main"), [{ list: transcript, words }, player] = await Promise.all([
            fetch(location.hash.substring(1))
                .then(response => response.json())
                .then(({ segments }) => {
                    const list = document.createElement("div");
                    return {
                        list,
                        words: segments.map(({ text, start, end, words }) => {
                            const line = document.createElement("div");
                            list.appendChild(line);
                            return words.map(({ word, start, end, score }) => {
                                const span = document.createElement("span");
                                span.textContent = word;
                                if (start || end) {
                                    span.segment = { start, end };
                                }
                                line.appendChild(span);
                                return span;
                            });
                        }).reduce((a, b) => a.concat(b), [])
                    };
                }),
            fetch("https://corsproxy.io/?" + encodeURIComponent(`https://player.vimeo.com/video/${vimeoId}/config`))
                .then(response => response.json())
                .then(({ request: { files: { progressive } } }) => progressive.reduce((a, b) => b.profile > a.profile ? a : b, {}).url)
                .then(url => new Promise((resolve, reject) => {
                    const audio = new Audio();
                    audio.controls = true;
                    audio.addEventListener("canplaythrough", e => resolve(e.target));
                    audio.addEventListener("error", e => reject(e.error));
                    audio.src = url;
                    return audio;
                }))
        ]);
        transcript.addEventListener("click", e => {
            let span = e.target;
            while (span) {
                if (span.segment) {
                    player.currentTime = span.segment.start;
                    return;
                }
                span = span.nextElementSibling || span.parentElement?.nextElementSibling?.firstElementChild;
            }
        });
        transcript.addEventListener("dblclick", e => player.play());
        let updateTimeout;
        player.addEventListener("timeupdate", function update(e) {
            clearTimeout(updateTimeout);
            updateTimeout = undefined;
            const t = e.target.currentTime;
            for (const lines of transcript.children) {
                lines.classList.remove("current");
            }
            for (const span of words) {
                if (span.segment && span.segment.start <= t && span.segment.end > t) {
                    span.classList.add("current");
                    span.parentElement.classList.add("current");
                    const { top, bottom } = span.getBoundingClientRect();
                    if (top < 0 || bottom > innerHeight - player.getBoundingClientRect().height) {
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
        });
        main.appendChild(transcript);
        main.appendChild(player);
    }
})().catch(console.error);