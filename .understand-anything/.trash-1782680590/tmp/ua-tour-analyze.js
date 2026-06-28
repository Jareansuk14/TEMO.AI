#!/usr/bin/env node
"use strict";

const fs = require("fs");

function main() {
  const inputPath = process.argv[2];
  const outputPath = process.argv[3];
  if (!inputPath || !outputPath) {
    console.error("Usage: ua-tour-analyze.js <input.json> <output.json>");
    process.exit(1);
  }

  const raw = JSON.parse(fs.readFileSync(inputPath, "utf8").replace(/^\uFEFF/, ""));

  // Keep only file-level nodes: types file / config / document
  const allowedTypes = new Set(["file", "config", "document"]);
  const nodes = (raw.nodes || []).filter((n) => allowedTypes.has(n.type));
  const nodeIds = new Set(nodes.map((n) => n.id));
  const layers = raw.layers || [];

  // Use all edges, but only those whose endpoints survive the file-level filter.
  const edges = (raw.edges || []).filter(
    (e) => nodeIds.has(e.source) && nodeIds.has(e.target)
  );

  // ---- Fan-in / Fan-out ----
  const fanIn = new Map();
  const fanOut = new Map();
  for (const id of nodeIds) {
    fanIn.set(id, 0);
    fanOut.set(id, 0);
  }
  for (const e of edges) {
    fanOut.set(e.source, fanOut.get(e.source) + 1);
    fanIn.set(e.target, fanIn.get(e.target) + 1);
  }

  const nameOf = (id) => {
    const n = nodes.find((x) => x.id === id);
    return n ? n.name : id;
  };
  const summaryOf = (id) => {
    const n = nodes.find((x) => x.id === id);
    return n ? n.summary || "" : "";
  };

  const fanInRanking = [...nodeIds]
    .map((id) => ({ id, fanIn: fanIn.get(id), name: nameOf(id) }))
    .sort((a, b) => b.fanIn - a.fanIn)
    .slice(0, 20);

  const fanOutRanking = [...nodeIds]
    .map((id) => ({ id, fanOut: fanOut.get(id), name: nameOf(id) }))
    .sort((a, b) => b.fanOut - a.fanOut)
    .slice(0, 20);

  // ---- Entry point candidates ----
  const entryNames = new Set([
    "index.ts","index.js","main.ts","main.js","app.ts","app.js",
    "server.ts","server.js","mod.rs","main.go","main.py","main.rs",
    "manage.py","app.py","wsgi.py","asgi.py","run.py","__main__.py",
    "Application.java","Main.java","Program.cs","config.ru","index.php",
    "App.swift","Application.kt","main.cpp","main.c",
  ]);

  const fanOutValues = [...fanOut.values()].sort((a, b) => a - b);
  const fanInValues = [...fanIn.values()].sort((a, b) => a - b);
  const pct = (arr, p) => {
    if (arr.length === 0) return 0;
    const idx = Math.floor((arr.length - 1) * p);
    return arr[idx];
  };
  const fanOutTop10 = pct(fanOutValues, 0.9);
  const fanInBottom25 = pct(fanInValues, 0.25);

  const entryScores = nodes.map((n) => {
    let score = 0;
    const fp = n.filePath || "";
    const depth = fp.split("/").length;
    if (n.type === "document") {
      if (n.name === "README.md" && depth === 1) score += 5;
      else if (/\.md$/i.test(n.name) && depth === 1) score += 2;
    } else {
      if (entryNames.has(n.name)) score += 3;
      if (depth <= 2) score += 1;
      if (fanOut.get(n.id) >= fanOutTop10) score += 1;
      if (fanIn.get(n.id) <= fanInBottom25) score += 1;
    }
    return { id: n.id, score, name: n.name, summary: n.summary || "" };
  });
  const entryPointCandidates = entryScores
    .sort((a, b) => b.score - a.score)
    .slice(0, 5);

  // ---- BFS from top code entry point ----
  const codeEntry = entryScores
    .filter((e) => {
      const n = nodes.find((x) => x.id === e.id);
      return n && n.type !== "document";
    })
    .sort((a, b) => b.score - a.score)[0];

  const startNode = codeEntry ? codeEntry.id : null;

  const traverseTypes = new Set(["imports", "calls"]);
  const adj = new Map();
  for (const id of nodeIds) adj.set(id, []);
  for (const e of edges) {
    if (traverseTypes.has(e.type)) adj.get(e.source).push(e.target);
  }

  const order = [];
  const depthMap = {};
  if (startNode) {
    const queue = [startNode];
    depthMap[startNode] = 0;
    const seen = new Set([startNode]);
    while (queue.length) {
      const cur = queue.shift();
      order.push(cur);
      for (const nxt of adj.get(cur) || []) {
        if (!seen.has(nxt)) {
          seen.add(nxt);
          depthMap[nxt] = depthMap[cur] + 1;
          queue.push(nxt);
        }
      }
    }
  }
  const byDepth = {};
  for (const [id, d] of Object.entries(depthMap)) {
    (byDepth[d] = byDepth[d] || []).push(id);
  }

  // ---- Non-code inventory ----
  const nonCodeFiles = {
    documentation: [],
    infrastructure: [],
    data: [],
    config: [],
  };
  for (const n of nodes) {
    const item = { id: n.id, name: n.name, summary: n.summary || "" };
    if (n.type === "document") nonCodeFiles.documentation.push(item);
    else if (n.type === "config") nonCodeFiles.config.push(item);
  }

  // ---- Clusters: bidirectional relationships expanded ----
  const pairKey = (a, b) => (a < b ? a + "||" + b : b + "||" + a);
  const edgeSet = new Set();
  const edgeBetween = new Map();
  for (const e of edges) {
    edgeSet.add(e.source + "->" + e.target);
    const k = pairKey(e.source, e.target);
    edgeBetween.set(k, (edgeBetween.get(k) || 0) + 1);
  }
  const clusterSeeds = [];
  for (const e of edges) {
    if (edgeSet.has(e.target + "->" + e.source)) {
      const key = pairKey(e.source, e.target);
      if (!clusterSeeds.find((c) => c.key === key)) {
        clusterSeeds.push({ key, nodes: new Set([e.source, e.target]) });
      }
    }
  }
  // Expand clusters
  for (const c of clusterSeeds) {
    for (const id of nodeIds) {
      if (c.nodes.has(id)) continue;
      let connections = 0;
      for (const m of c.nodes) {
        if (edgeSet.has(id + "->" + m) || edgeSet.has(m + "->" + id)) connections++;
      }
      if (connections >= 2 && c.nodes.size < 5) c.nodes.add(id);
    }
  }
  const clusters = clusterSeeds
    .map((c) => {
      const arr = [...c.nodes];
      let edgeCount = 0;
      for (let i = 0; i < arr.length; i++)
        for (let j = 0; j < arr.length; j++)
          if (i !== j && edgeSet.has(arr[i] + "->" + arr[j])) edgeCount++;
      return { nodes: arr, edgeCount };
    })
    .sort((a, b) => b.edgeCount - a.edgeCount)
    .slice(0, 10);

  // ---- Node summary index ----
  const nodeSummaryIndex = {};
  for (const n of nodes) {
    nodeSummaryIndex[n.id] = {
      name: n.name,
      type: n.type,
      summary: n.summary || "",
    };
  }

  const result = {
    scriptCompleted: true,
    entryPointCandidates,
    fanInRanking,
    fanOutRanking,
    bfsTraversal: { startNode, order, depthMap, byDepth },
    nonCodeFiles,
    clusters,
    layers: { count: layers.length, list: layers.map((l) => ({ id: l.id, name: l.name, description: l.description })) },
    nodeSummaryIndex,
    totalNodes: nodes.length,
    totalEdges: edges.length,
  };

  fs.writeFileSync(outputPath, JSON.stringify(result, null, 2));
  console.log("Tour analysis written. nodes=" + nodes.length + " edges=" + edges.length + " startNode=" + startNode);
  process.exit(0);
}

try {
  main();
} catch (err) {
  console.error(err.stack || String(err));
  process.exit(1);
}
