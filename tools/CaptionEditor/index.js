// e.g. #https://raw.githubusercontent.com/TimmyWong-TW/Maize-KB/main/whisperx/vimeo/%E6%B7%A1%E6%B1%9F%E6%95%99%E6%9C%83/%E4%B8%BB%E6%97%A5%E4%BF%A1%E6%81%AF/6158626%3D090809%20%E9%A0%86%E6%9C%8D%E7%9A%84%E9%A0%98%E8%A2%96%20%E8%8E%8A%E8%82%B2%E9%8A%98%E7%89%A7%E5%B8%AB%20%EF%BC%8F%20%E6%B0%91%E6%95%B8%E8%A8%98.json
(async () => {
    const vimeoId = location.hash?.match(/(?<=\/vimeo\/.+\/)\d+(?=(=|%3D)[^\/]+$)/)?.[0];
    if (vimeoId) {
        const [table] = document.getElementsByTagName("table");
        const audio = document.getElementById("audio");
        fetch(location.hash.substring(1))
            .then(response => response.json())
            .then(({ segments }) => {
                table.textContent = "";
                segments.map(s => {
                    const tr = document.createElement("tr"),
                        tdStart = document.createElement("td"),
                        tdEnd = document.createElement("td"),
                        tdText = document.createElement("td");
                    tdStart.textContent = s.start.toString();
                    tdEnd.textContent = s.end.toString();
                    tdText.textContent = s.text;
                    tr.appendChild(tdStart);
                    tr.appendChild(tdEnd);
                    tr.appendChild(tdText);
                    table.appendChild(tr);
                });
            });
        fetch("https://corsproxy.io/?" + encodeURIComponent(`https://player.vimeo.com/video/${vimeoId}/config`))
            .then(response => response.json())
            .then(({ request: { files: { progressive } } }) => progressive.reduce((a, b) => b.profile > a.profile ? a : b, {}).url)
            .then(url => audio.src = url);
    }
})();