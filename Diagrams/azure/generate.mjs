import { readFileSync, writeFileSync } from 'node:fs';
import { dirname, join } from 'node:path';
import { fileURLToPath } from 'node:url';

const root = dirname(fileURLToPath(import.meta.url));
const width = 3000;
const height = 1900;
const escape = value => String(value).replaceAll('&', '&amp;').replaceAll('<', '&lt;').replaceAll('>', '&gt;').replaceAll('"', '&quot;');
const icon = name => `data:image/svg+xml;base64,${readFileSync(join(root, 'icons', `${name}.svg`)).toString('base64')}`;

function diagram(async) {
    const parts = [];
    const text = (x, y, value, size = 24, color = '#334155', weight = 400, anchor = 'start') => {
        parts.push(`<text x="${x}" y="${y}" font-size="${size}" fill="${color}" font-weight="${weight}" text-anchor="${anchor}">${escape(value)}</text>`);
    };
    const box = (x, y, w, h, fill, stroke = '#cbd5e1', radius = 18, dashed = false) => {
        parts.push(`<rect x="${x}" y="${y}" width="${w}" height="${h}" rx="${radius}" fill="${fill}" stroke="${stroke}" stroke-width="2"${dashed ? ' stroke-dasharray="10 8"' : ''}/>`);
    };
    const card = (x, y, w, h, image, title, lines, role = 'normal') => {
        const colors = {
            normal: ['#ffffff', '#cbd5e1'],
            external: ['#f1f3f5', '#adb5bd'],
            bus: ['#eff7ff', '#3b82c4'],
            processor: ['#f7f2fc', '#8064a2'],
        };
        box(x, y, w, h, ...colors[role]);
        parts.push(`<image href="${icon(image)}" x="${x + 22}" y="${y + 22}" width="66" height="66" preserveAspectRatio="xMidYMid meet"/>`);
        text(x + 106, y + 49, title, 25, '#0f172a', 650);
        lines.forEach((line, i) => text(x + 22, y + 113 + i * 29, line, 22));
    };
    const edge = (points, color = '#475569', dashed = false, both = false) => {
        parts.push(`<polyline points="${points}" fill="none" stroke="${color}" stroke-width="3"${dashed ? ' stroke-dasharray="9 7"' : ''} marker-end="url(#${color === '#94a3b8' ? 'muted' : 'arrow'})"${both ? ' marker-start="url(#arrow)"' : ''} stroke-linejoin="round"/>`);
    };
    const label = (x, y, value, size = 22, color = '#334155') => {
        const w = value.length * size * 0.54 + 24;
        parts.push(`<rect x="${x - w / 2}" y="${y - size}" width="${w}" height="${size + 12}" rx="5" fill="#ffffff" fill-opacity="0.96"/>`);
        text(x, y, value, size, color, 500, 'middle');
    };

    parts.push(`<svg xmlns="http://www.w3.org/2000/svg" width="${width}" height="${height}" viewBox="0 0 ${width} ${height}">
<defs>
<marker id="arrow" markerWidth="12" markerHeight="12" refX="10" refY="5" orient="auto-start-reverse" markerUnits="userSpaceOnUse"><path d="M0 0 L10 5 L0 10 Z" fill="#475569"/></marker>
<marker id="muted" markerWidth="12" markerHeight="12" refX="10" refY="5" orient="auto-start-reverse" markerUnits="userSpaceOnUse"><path d="M0 0 L10 5 L0 10 Z" fill="#94a3b8"/></marker>
</defs><g font-family="Segoe UI, Arial, sans-serif">`);
    box(0, 0, width, height, '#ffffff', '#ffffff', 0);
    text(60, 80, `KnowledgeGram | ${async ? 'Asynchronous' : 'Synchronous'} Azure architecture`, 48, '#0f172a', 700);
    text(60, 125, 'Planned target deployment • Azure service inventory and integration paths • Not the current application implementation', 25, '#64748b');

    box(400, 340, 2110, 1200, '#f8fafc', '#94a3b8', 24);
    text(430, 379, 'KNOWLEDGEGRAM PLATFORM', 23, '#475569', 700);
    text(2545, 425, 'EXTERNAL SERVICES', 22, '#64748b', 700);

    edge('300,760 450,760');
    label(375, 735, 'HTTPS', 20);
    edge('710,760 860,760');
    label(785, 735, 'HTTPS', 20);
    edge('170,680 170,215 860,215', '#94a3b8', true);
    label(490, 196, 'Clients + administrators sign in', 22, '#64748b');
    edge('1020,680 1020,285', '#94a3b8', true);
    label(1090, 447, 'Sign-in', 20, '#64748b');

    edge('1020,1010 1020,860');
    label(1015, 933, 'Check preferences', 21);
    label(1015, 964, '+ new content', 21);

    edge('1180,835 1245,835 1245,1240 1430,1240 1430,1310');
    edge('1245,1240 1880,1240 1880,1310');
    label(1550, 1225, async ? '5  Web App persists content / metadata / files' : 'Web App reads / writes content and files', 22);

    if (async) {
        edge('1180,710 1220,710 1220,575 1330,575');
        label(1255, 695, '1  Enqueue', 20);
        edge('1670,575 1840,575');
        label(1755, 545, '2  Consume', 21);
        edge('2180,575 2620,575', '#475569', false, true);
        label(2400, 542, '3  Retrieve content', 22);
        edge('2010,480 2010,410 1100,410 1100,680');
        label(1600, 434, '4  Submit results to Web App for persistence', 22);

        edge('1180,790 1290,790 1290,1100 1330,1100');
        label(1305, 906, 'Enqueue', 20);
        edge('1180,1100 1330,1100');
        label(1252, 1075, 'Enqueue', 20);
        edge('1670,1100 1840,1100');
        label(1755, 1070, 'Consume', 21);
        edge('2180,1100 2620,1100');
        label(2390, 1070, 'Send notification', 22);

        card(1330, 480, 340, 190, 'service-bus', 'Azure Service Bus', ['Learning namespace', '1 learning request queue'], 'bus');
        card(1840, 480, 340, 190, 'function-app', 'Function App', ['Learning Integration', 'Process learning requests'], 'processor');
        card(1330, 1010, 340, 190, 'service-bus', 'Azure Service Bus', ['Notification namespace', '1 notification queue'], 'bus');
        card(1840, 1010, 340, 190, 'function-app', 'Function App', ['Notification Processor', 'Process notification requests'], 'processor');

        text(1350, 755, 'LEARNING IMPORT', 22, '#3b82c4', 700);
        text(1350, 791, 'Accepted does not mean retrieved or saved.', 22, '#64748b');
        text(1350, 840, 'Results always return through the Web App.', 22, '#64748b');
        text(1350, 876, 'No function-to-database / blob writes.', 22, '#64748b');
    } else {
        edge('1180,710 1470,710 1470,575 2620,575', '#475569', false, true);
        label(2060, 545, 'Direct content retrieval (request / response)', 23);
        edge('1180,790 2370,790 2370,1100 2620,1100');
        label(1800, 763, 'Web App sends notification directly', 23);
        edge('1180,1100 2370,1100');
        label(1790, 1070, 'Matching-content notification sent directly', 23);
        text(1460, 915, 'DIRECT INTEGRATIONS', 23, '#475569', 700);
        text(1460, 954, 'No Service Bus namespaces or processing Function Apps.', 22, '#64748b');
        text(1460, 989, 'Provider acceptance is not proof of recipient delivery.', 22, '#64748b');
    }

    card(40, 680, 260, 180, 'users', 'Users', ['Clients', 'Administrators']);
    card(450, 680, 260, 180, 'front-door', 'Front Door', ['Azure Front Door', 'Tier / WAF unspecified']);
    card(860, 680, 320, 180, 'app-service', 'App Service', ['KnowledgeGram Web App', 'UI + application endpoints']);
    card(860, 145, 320, 140, 'entra-id', 'Entra ID', ['One shared identity provider'], 'external');
    card(860, 1010, 320, 190, 'function-app', 'Function App', ['Content Notification', 'Match content to preferences']);
    card(1260, 1310, 340, 170, 'sql-database', 'Azure SQL Database', ['Content Database', 'Microsoft SQL engine']);
    card(1710, 1310, 340, 170, 'blob-storage', 'Storage Account', ['Azure Blob Storage', 'Uploaded / imported content']);
    card(2620, 480, 320, 190, 'learn', 'Microsoft Learn', ['External knowledge solution', 'API / contract to confirm'], 'external');
    card(2620, 1010, 320, 190, 'sendgrid', 'SendGrid', ['External notification service', 'Send e-mail notifications'], 'external');

    box(400, 1580, 2110, 240, '#ffffff', '#cbd5e1', 20, true);
    text(430, 1620, 'SHARED APPLICATION SUPPORT', 22, '#475569', 700);
    card(860, 1650, 400, 145, 'key-vault', 'Azure Key Vault', ['Application / provider secrets']);
    card(1490, 1650, 440, 145, 'application-insights', 'Application Insights', ['Application logs, metrics, traces']);
    text(430, 1687, 'Used by App Service', 22);
    text(430, 1720, 'and all Function Apps.', 22);
    text(2040, 1687, 'Support dependencies only;', 22);
    text(2040, 1720, 'not business-message routes.', 22);
    text(430, 1521, async ? 'Inventory: 1 App Service • 3 Function Apps • 2 Service Bus namespaces, 1 queue each' : 'Inventory: 1 App Service • 1 Function App • No message buses', 22, '#64748b');
    text(60, 1867, 'Official Microsoft architecture icons • Arrows show logical flow, not network topology • Managed identities, regions and private networking are not depicted', 22, '#64748b');
    parts.push('</g></svg>');
    return parts.join('\n');
}

for (const alternative of ['sync', 'async']) {
    const svg = diagram(alternative === 'async');
    const name = `knowledgegram-azure-${alternative}`;
    writeFileSync(join(root, `${name}.svg`), svg);
    writeFileSync(join(root, `${name}.html`), `<!doctype html><html><head><meta charset="utf-8"><style>html,body{margin:0;width:${width}px;height:${height}px;overflow:hidden}svg{display:block}</style></head><body>${svg}</body></html>`);
}
