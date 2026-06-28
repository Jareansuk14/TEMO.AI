const fs = require('fs');
const dir = 'c:/MainProject/Autoweb/TEMO.AI/.understand-anything/intermediate';

function readJson(p) { return JSON.parse(fs.readFileSync(p, 'utf8').replace(/^\uFEFF/, '')); }
const graph = readJson(dir + '/assembled-graph.json');
let layers = readJson(dir + '/layers.json');
let tour = readJson(dir + '/tour.json');

const nodeIds = new Set(graph.nodes.map(n => n.id));
const KNOWN_PREFIXES = ['file:', 'config:', 'document:', 'service:', 'pipeline:', 'table:', 'schema:', 'resource:', 'endpoint:', 'function:', 'class:'];
function hasPrefix(id) { return KNOWN_PREFIXES.some(p => id.startsWith(p)); }
function toFileId(id) { return hasPrefix(id) ? id : ('file:' + id); }

// --- Normalize layers ---
if (!Array.isArray(layers) && Array.isArray(layers.layers)) layers = layers.layers;
layers = layers.map(l => {
  let ids = l.nodeIds || l.nodes || [];
  ids = ids.map(x => (typeof x === 'string' ? x : (x && x.id))).filter(Boolean).map(toFileId);
  ids = ids.filter(id => nodeIds.has(id));
  const id = l.id || ('layer:' + String(l.name || 'layer').toLowerCase().replace(/[^a-z0-9]+/g, '-').replace(/^-|-$/g, ''));
  return { id, name: l.name, description: l.description, nodeIds: ids };
});

// --- Normalize tour ---
if (!Array.isArray(tour) && Array.isArray(tour.steps)) tour = tour.steps;
tour = tour.map(s => {
  let ids = s.nodeIds || s.nodesToInspect || [];
  ids = ids.map(toFileId).filter(id => nodeIds.has(id));
  const step = {
    order: s.order,
    title: s.title,
    description: s.description || s.whyItMatters,
    nodeIds: ids,
  };
  if (s.languageLesson) step.languageLesson = s.languageLesson;
  return step;
}).sort((a, b) => a.order - b.order);

const kg = {
  version: '1.0.0',
  project: {
    name: 'TEMO.AI',
    languages: ['batch', 'csharp', 'markdown', 'xaml'],
    frameworks: ['WPF', 'Windows Forms', 'WebView2', 'SkiaSharp', 'Velopack'],
    description: 'A WPF/.NET 9 Windows desktop application for AI-assisted website generation: it composes layouts and sections, generates content, CSS and images via OpenAI, manages projects and themes, and deploys generated sites to Vercel.',
    analyzedAt: new Date().toISOString(),
    gitCommitHash: 'a3f5437c3e3f2d53868435fdd076f8eba16349f1',
  },
  nodes: graph.nodes,
  edges: graph.edges,
  layers,
  tour,
};

fs.writeFileSync(dir + '/assembled-graph.json', JSON.stringify(kg, null, 2));
console.log('Assembled KG: nodes=' + kg.nodes.length + ' edges=' + kg.edges.length + ' layers=' + kg.layers.length + ' tourSteps=' + kg.tour.length);
