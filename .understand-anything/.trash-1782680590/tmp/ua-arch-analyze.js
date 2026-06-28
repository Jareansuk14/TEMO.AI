#!/usr/bin/env node
"use strict";

const fs = require("fs");

const graphPath = process.argv[2];
const outPath = process.argv[3];

function fail(msg) {
  console.error(msg);
  process.exit(1);
}

if (!graphPath || !outPath) fail("Usage: node ua-arch-analyze.js <graph.json> <out.json>");

let graph;
try {
  graph = JSON.parse(fs.readFileSync(graphPath, "utf8").replace(/^\uFEFF/, ""));
} catch (e) {
  fail("Failed to parse graph JSON: " + e.message);
}

const FILE_LEVEL = new Set(["file", "config", "document", "service", "pipeline", "table", "schema", "resource", "endpoint"]);

const allNodes = graph.nodes || [];
const allEdges = graph.edges || [];

// File-level nodes only
const fileNodes = allNodes.filter((n) => FILE_LEVEL.has(n.type));
const fileNodeIds = new Set(fileNodes.map((n) => n.id));

// File-level edges only (both endpoints are file-level nodes)
const fileEdges = allEdges.filter((e) => fileNodeIds.has(e.source) && fileNodeIds.has(e.target));

// Import-like edges
const IMPORT_TYPES = new Set(["imports", "depends_on", "uses", "references", "calls"]);
const importEdges = fileEdges.filter((e) => IMPORT_TYPES.has(e.type));

// --- Common prefix detection ---
function topDir(filePath) {
  const norm = filePath.replace(/\\/g, "/");
  const parts = norm.split("/");
  if (parts.length === 1) return "(root)";
  return parts[0];
}

// A. Directory grouping
const directoryGroups = {};
const idToGroup = {};
for (const n of fileNodes) {
  const g = topDir(n.filePath || n.name || "");
  (directoryGroups[g] = directoryGroups[g] || []).push(n.id);
  idToGroup[n.id] = g;
}

// B. Node type grouping
const nodeTypeGroups = {};
for (const n of fileNodes) {
  (nodeTypeGroups[n.type] = nodeTypeGroups[n.type] || []).push(n.id);
}

// C. fan-in / fan-out (import edges)
const fileFanIn = {};
const fileFanOut = {};
for (const id of fileNodeIds) {
  fileFanIn[id] = 0;
  fileFanOut[id] = 0;
}
for (const e of importEdges) {
  fileFanOut[e.source] = (fileFanOut[e.source] || 0) + 1;
  fileFanIn[e.target] = (fileFanIn[e.target] || 0) + 1;
}

// D. Cross-category edges (by node type, using all file-level edges)
const typeOf = {};
for (const n of fileNodes) typeOf[n.id] = n.type;
const crossCatMap = {};
for (const e of fileEdges) {
  const ft = typeOf[e.source];
  const tt = typeOf[e.target];
  if (ft === tt && ft === "file") continue; // skip plain file->file for cross category
  const key = ft + "|" + tt + "|" + e.type;
  crossCatMap[key] = (crossCatMap[key] || 0) + 1;
}
const crossCategoryEdges = Object.entries(crossCatMap).map(([k, count]) => {
  const [fromType, toType, edgeType] = k.split("|");
  return { fromType, toType, edgeType, count };
});

// E. Inter-group import frequency
const interMap = {};
for (const e of importEdges) {
  const fg = idToGroup[e.source];
  const tg = idToGroup[e.target];
  if (fg === tg) continue;
  const key = fg + "|" + tg;
  interMap[key] = (interMap[key] || 0) + 1;
}
const interGroupImports = Object.entries(interMap)
  .map(([k, count]) => {
    const [from, to] = k.split("|");
    return { from, to, count };
  })
  .sort((a, b) => b.count - a.count);

// F. Intra-group density
const intraGroupDensity = {};
for (const g of Object.keys(directoryGroups)) {
  intraGroupDensity[g] = { internalEdges: 0, totalEdges: 0, density: 0 };
}
for (const e of importEdges) {
  const fg = idToGroup[e.source];
  const tg = idToGroup[e.target];
  if (fg) intraGroupDensity[fg].totalEdges++;
  if (tg && tg !== fg) intraGroupDensity[tg].totalEdges++;
  if (fg && fg === tg) intraGroupDensity[fg].internalEdges++;
}
for (const g of Object.keys(intraGroupDensity)) {
  const d = intraGroupDensity[g];
  d.density = d.totalEdges ? +(d.internalEdges / d.totalEdges).toFixed(3) : 0;
}

// G. Pattern matching
const DIR_PATTERNS = [
  [/^(routes|api|controllers|endpoints|handlers|serializers|blueprints|routers|controller)$/i, "api"],
  [/^(services|core|lib|domain|logic|signals|composables|mailers|jobs|channels|internal)$/i, "service"],
  [/^(models|db|data|persistence|repository|entities|entity|migrations|sql|database)$/i, "data"],
  [/^(components|views|pages|ui|layouts|screens|controls)$/i, "ui"],
  [/^(middleware|plugins|interceptors|guards)$/i, "middleware"],
  [/^(utils|helpers|common|shared|tools|pkg|templatetags)$/i, "utility"],
  [/^(config|constants|env|settings|management|commands)$/i, "config"],
  [/^(__tests__|test|tests|spec|specs)$/i, "test"],
  [/^(types|interfaces|schemas|contracts|dtos|dto|request|response)$/i, "types"],
  [/^hooks$/i, "hooks"],
  [/^(store|state|reducers|actions|slices)$/i, "state"],
  [/^(assets|static|public)$/i, "assets"],
  [/^(cmd|bin)$/i, "entry"],
  [/^(docs|documentation|wiki)$/i, "documentation"],
  [/^(deploy|deployment|infra|infrastructure|docker|k8s|kubernetes|helm|charts|terraform|tf)$/i, "infrastructure"],
  [/^(\.github|\.gitlab|\.circleci)$/i, "ci-cd"],
  [/^themes$/i, "ui"],
];
const patternMatches = {};
for (const g of Object.keys(directoryGroups)) {
  let label = null;
  for (const [re, lbl] of DIR_PATTERNS) {
    if (re.test(g)) { label = lbl; break; }
  }
  if (label) patternMatches[g] = label;
}

// H. Deployment topology
const infraFiles = [];
let hasDockerfile = false, hasCompose = false, hasK8s = false, hasTerraform = false, hasCI = false;
for (const n of fileNodes) {
  const fp = (n.filePath || "").replace(/\\/g, "/");
  const base = fp.split("/").pop();
  if (/^Dockerfile/i.test(base)) { hasDockerfile = true; infraFiles.push(fp); }
  if (/^docker-compose/i.test(base)) { hasCompose = true; infraFiles.push(fp); }
  if (/\.tf$|\.tfvars$/i.test(base)) { hasTerraform = true; infraFiles.push(fp); }
  if (/workflows\//i.test(fp) || /\.gitlab-ci\.yml$/i.test(base) || /^Jenkinsfile$/i.test(base)) { hasCI = true; infraFiles.push(fp); }
  if (/\.(bat|sh|ps1)$/i.test(base)) { infraFiles.push(fp); }
}

// I. Data pipeline detection (minimal)
const dataPipeline = { schemaFiles: [], migrationFiles: [], dataModelFiles: [], apiHandlerFiles: [] };
for (const n of fileNodes) {
  const fp = (n.filePath || "").replace(/\\/g, "/");
  if (/^Models\//i.test(fp)) dataPipeline.dataModelFiles.push(fp);
}

// J. Doc coverage
const docFilesByGroup = {};
for (const n of fileNodes) {
  if (n.type === "document" || /\.md$/i.test(n.filePath || "")) {
    const g = idToGroup[n.id];
    docFilesByGroup[g] = true;
  }
}
const totalGroups = Object.keys(directoryGroups).length;
const groupsWithDocs = Object.keys(docFilesByGroup).length;
const undocumentedGroups = Object.keys(directoryGroups).filter((g) => !docFilesByGroup[g]);

// K. Dependency direction
const pairSeen = {};
const dependencyDirection = [];
for (const { from, to, count } of interGroupImports) {
  const a = from, b = to;
  const key = [a, b].sort().join("|");
  if (pairSeen[key]) continue;
  pairSeen[key] = true;
  const fwd = interMap[a + "|" + b] || 0;
  const rev = interMap[b + "|" + a] || 0;
  if (fwd >= rev) dependencyDirection.push({ dependent: a, dependsOn: b });
  else dependencyDirection.push({ dependent: b, dependsOn: a });
}

// File stats
const filesPerGroup = {};
for (const g of Object.keys(directoryGroups)) filesPerGroup[g] = directoryGroups[g].length;
const nodeTypeCounts = {};
for (const t of Object.keys(nodeTypeGroups)) nodeTypeCounts[t] = nodeTypeGroups[t].length;

const result = {
  scriptCompleted: true,
  directoryGroups,
  nodeTypeGroups,
  crossCategoryEdges: crossCategoryEdges.sort((a, b) => b.count - a.count),
  interGroupImports,
  intraGroupDensity,
  patternMatches,
  deploymentTopology: {
    hasDockerfile, hasCompose, hasK8s, hasTerraform, hasCI,
    infraFiles: Array.from(new Set(infraFiles)),
  },
  dataPipeline,
  docCoverage: {
    groupsWithDocs,
    totalGroups,
    coverageRatio: totalGroups ? +(groupsWithDocs / totalGroups).toFixed(2) : 0,
    undocumentedGroups,
  },
  dependencyDirection,
  fileStats: {
    totalFileNodes: fileNodes.length,
    filesPerGroup,
    nodeTypeCounts,
  },
  fileFanIn,
  fileFanOut,
};

fs.writeFileSync(outPath, JSON.stringify(result, null, 2), "utf8");
console.log("Wrote results. Total file nodes: " + fileNodes.length);
process.exit(0);
