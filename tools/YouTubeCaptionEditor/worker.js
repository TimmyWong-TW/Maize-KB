// https://youtube.timtim.workers.dev/
export default {
  async fetch(request, env, ctx) {
    const origin = request.headers.get('Origin');
    if (request.method === 'OPTIONS') {
      return new Response(null, {
        headers: origin !== null &&
          request.headers.get('Access-Control-Request-Method') !== null &&
          request.headers.get('Access-Control-Request-Headers') !== null ? {
          'Access-Control-Allow-Origin': origin,
          'Access-Control-Allow-Methods': 'GET,HEAD,POST,OPTIONS',
          'Access-Control-Max-Age': '86400',
          'Access-Control-Allow-Headers': request.headers.get('Access-Control-Request-Headers'),
        } : {
          Allow: "GET, HEAD, POST, OPTIONS",
        }
      });
    }
    const v = new URL(request.url).searchParams.get('v');
    if (!v) {
      return new Response(null, {
        status: 400
      });
    }
    try {
      const urls = await fetch(`https://www.youtube.com/watch?v=${v}`)
        .then(response => response.text())
        .then(html => {
          const { streamingData: { adaptiveFormats } } = JSON.parse(html.match(/(?<=\bytInitialPlayerResponse\s*=\s*){.+?}(?=\s*;[^<]*<\/script>?)/s)[0]);
          return {
            adaptiveFormats,
            player: `https://www.youtube.com${html.match(/(?<=<script[^>]+\bsrc=")\/s\/player\/[^"]+\/base.js/)[0]}`
          };
        })
        .then(async ({ adaptiveFormats, player }) => adaptiveFormats.map(f => f.url ? f : fetch(player)
          .then(response => response.text())
          .then(script => {
            const scope = script.substring(script.indexOf('{', script.indexOf(';') + 1), script.lastIndexOf(')', script.lastIndexOf('_yt_player') - 1)),
              sDecipher = scope.match(/(?<=\b[cs]\s*&&\s*[adf]\.set\([^,]+\s*,\s*encodeURIComponent\s*\(\s*)[a-zA-Z0-9$]+(?=\()|(?<=\b[a-zA-Z0-9]+\s*&&\s*[a-zA-Z0-9]+\.set\([^,]+\s*,\s*encodeURIComponent\s*\(\s*)[a-zA-Z0-9$]+(?=\()|(?<=\bm=)[a-zA-Z0-9$]{2,}(?=\(decodeURIComponent\(h\.s\)\))|(?<=\bc&&\(c=)[a-zA-Z0-9$]{2,}(?=\(decodeURIComponent\(c\)\))|(?<=\b|[^a-zA-Z0-9$])[a-zA-Z0-9$]{2,}(?=\s*=\s*function\(\s*a\s*\)\s*{\s*a\s*=\s*a\.split\(\s*""\s*\)(?:;[a-zA-Z0-9$]{2}\.[a-zA-Z0-9$]{2}\(a,\d+\))?)|[a-zA-Z0-9$]+(?=\s*=\s*function\(\s*a\s*\)\s*{\s*a\s*=\s*a\.split\(\s*""\s*\))|(?<=("|')signature\1\s*,\s*)[a-zA-Z0-9$]+(?=\()|(?<=\.sig\|\|)[a-zA-Z0-9$]+(?=\()|(?<=yt\.akamaized\.net\/\)\s*\|\|\s*.*?\s*[cs]\s*&&\s*[adf]\.set\([^,]+\s*,\s*(?:encodeURIComponent\s*\()?\s*)[a-zA-Z0-9$]+(?=\()|(?<=\b[cs]\s*&&\s*[adf]\.set\([^,]+\s*,\s*)[a-zA-Z0-9$]+(?=\()|(?<=\b[a-zA-Z0-9]+\s*&&\s*[a-zA-Z0-9]+\.set\([^,]+\s*,\s*)[a-zA-Z0-9$]+(?=\()|(?<=\bc\s*&&\s*[a-zA-Z0-9]+\.set\([^,]+\s*,\s*\([^)]*\)\s*\(\s*)[a-zA-Z0-9$]+(?=\()/)[0],
              nDecihper = scope.match(/(?<=\.get\("n"\)\)&&\(b=)[a-zA-Z0-9$]+(?:\[\d+\])?(?=\([a-zA-Z0-9]\))/)[0],
              { s, sp, url } = f.signatureCipher.split('&').reduce((q, p) => {
                const [k, v] = p.split('=', 2);
                q[k] = decodeURI(v);
                return q;
              }, {}),
              n = url.split('&').find(p => p.match(/^n=/))?.substring(2),
              [sig, nonce] = new Function(`g={};${scope}return[${sDecipher}('${s}'),${nDecihper}('${n}')]`)();
            f.url = `${url.replace('n=' + n, 'n=' + nonce)}&${sp}=${sig}`;
            return f;
          })));
      return new Response(JSON.stringify(urls), {
        headers: {
          'Access-Control-Allow-Origin': origin,
          'Vary': 'Origin',
          'Content-Type': 'application/json'
        }
      });
    } catch (e) {
      return new Response(e, {
        status: 500,
        headers: {
          'Access-Control-Allow-Origin': origin,
          'Vary': 'Origin'
        }
      });
    }
  }
};