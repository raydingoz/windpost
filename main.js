import * as THREE from 'https://cdn.jsdelivr.net/npm/three@0.182.0/build/three.module.min.js';

/**
 * Windpost – Sail/Wind Physics v5
 * Rebuilt:
 * - Consistent axes (heading=0 -> +Z)
 * - Rudder affects yaw (heading), and strong lateral resistance keeps velocity aligned with heading
 * - Sail force computed from apparent wind (lift + drag), with soft stall and a no-go penalty
 * - Boat model rebuilt from scratch (procedural shape + extrude)
 * - Sunset lighting + env reflections + procedural normal map water
 * - Pixel-ish rendering (low internal res)
 */

const clamp = (v, a, b) => Math.max(a, Math.min(b, v));
const wrapAngle = (a) => {
  while (a > Math.PI) a -= Math.PI * 2;
  while (a < -Math.PI) a += Math.PI * 2;
  return a;
};

let showHud = true;

// DOM
const canvasHud = document.getElementById('hud');
const hudCtx = canvasHud.getContext('2d');

const hudEls = {
  rudderNeedle: document.getElementById('rudderNeedle'),
  sailNeedle: document.getElementById('sailNeedle'),
  trueWindArrow: document.getElementById('trueWindArrow'),
  appWindArrow: document.getElementById('appWindArrow'),
  rudderValue: document.getElementById('rudderValue'),
  sailValue: document.getElementById('sailValue'),
  trueWindValue: document.getElementById('trueWindValue'),
  appWindValue: document.getElementById('appWindValue'),
  speedValue: document.getElementById('speedValue'),
  headingValue: document.getElementById('headingValue'),
  sailStateValue: document.getElementById('sailStateValue'),
  sailFill: document.getElementById('sailFill'),
  engineValue: document.getElementById('engineValue'),
  throttleFill: document.getElementById('throttleFill'),
  windEffValue: document.getElementById('windEffValue'),
  windEffFill: document.getElementById('windEffFill'),
};

// Scene
const scene = new THREE.Scene();
scene.fog = new THREE.Fog(0x1a4a6b, 85, 520);

const camera = new THREE.PerspectiveCamera(55, 1, 0.1, 2500);
camera.position.set(0, 7, -14);

const renderer = new THREE.WebGLRenderer({ antialias: false, powerPreference: 'high-performance' });
renderer.setPixelRatio(1);
renderer.outputColorSpace = THREE.SRGBColorSpace;
renderer.toneMapping = THREE.ACESFilmicToneMapping;
renderer.toneMappingExposure = 1.12;
renderer.setClearColor(0x193b5f, 1);
document.body.appendChild(renderer.domElement);

// Environment (PMREM from a procedural equirectangular gradient)
function makeEnvTexture(){
  const c = document.createElement('canvas');
  c.width = 256; c.height = 128;
  const ctx = c.getContext('2d');
  const g = ctx.createLinearGradient(0, 0, 0, 128);
  g.addColorStop(0.00, '#10315e');
  g.addColorStop(0.45, '#6a4b6b');
  g.addColorStop(0.70, '#f1b15a');
  g.addColorStop(1.00, '#2b3b5d');
  ctx.fillStyle = g;
  ctx.fillRect(0, 0, 256, 128);
  ctx.fillStyle = 'rgba(255, 210, 150, 0.32)';
  ctx.fillRect(0, 84, 256, 10);

  const tex = new THREE.CanvasTexture(c);
  tex.colorSpace = THREE.SRGBColorSpace;
  tex.mapping = THREE.EquirectangularReflectionMapping;
  tex.magFilter = THREE.LinearFilter;
  tex.minFilter = THREE.LinearFilter;
  return tex;
}
const pmrem = new THREE.PMREMGenerator(renderer);
const envTex = makeEnvTexture();
const envRT = pmrem.fromEquirectangular(envTex);
scene.environment = envRT.texture;

// Sky dome (shader)
const skyGeo = new THREE.SphereGeometry(1100, 32, 18);
const skyVert = `
varying vec3 vPos;
void main(){
  vPos = position;
  gl_Position = projectionMatrix * modelViewMatrix * vec4(position, 1.0);
}
`;
const skyFrag = `
varying vec3 vPos;
void main(){
  float h = normalize(vPos).y * 0.5 + 0.5;

  vec3 top = vec3(0.16, 0.30, 0.58);
  vec3 mid = vec3(0.55, 0.48, 0.50);
  vec3 hor = vec3(0.98, 0.82, 0.50);

  vec3 col = mix(hor, mid, smoothstep(0.00, 0.40, h));
  col = mix(col, top, smoothstep(0.40, 1.00, h));

  vec3 sunDir = normalize(vec3(-0.92, 0.30, 0.20));
  float s = max(dot(normalize(vPos), sunDir), 0.0);
  col += vec3(1.0, 0.55, 0.20) * pow(s, 64.0) * 0.65;

  gl_FragColor = vec4(col, 1.0);
}
`;
const skyMat = new THREE.ShaderMaterial({ vertexShader: skyVert, fragmentShader: skyFrag, side: THREE.BackSide, depthWrite: false });
scene.add(new THREE.Mesh(skyGeo, skyMat));

// Lighting (sunset)
scene.add(new THREE.HemisphereLight(0x9fc4ff, 0x1a4a6b, 0.62));
const sun = new THREE.DirectionalLight(0xffd29a, 2.35);
sun.position.set(-45, 18, 12);
scene.add(sun);

const fill = new THREE.DirectionalLight(0x9ad2ff, 0.55);
fill.position.set(35, 60, -30);
scene.add(fill);

const boatKey = new THREE.PointLight(0xffe3c7, 0.70, 32, 2);
boatKey.position.set(0, 4.0, 0);
scene.add(boatKey);

// Helpers (true wind arrow above boat)
const windArrow = new THREE.ArrowHelper(new THREE.Vector3(0,0,1), new THREE.Vector3(0, 2.8, 0), 5.0, 0x78c8ff);
scene.add(windArrow);

// Water surface
const params = { internalScale: 0.34, waterSize: 900 };
const waterGeo = new THREE.PlaneGeometry(params.waterSize, params.waterSize, 80, 80);
waterGeo.rotateX(-Math.PI / 2);

function makeWaterTexture() {
  const c = document.createElement('canvas');
  c.width = 64; c.height = 64;
  const ctx = c.getContext('2d');
  ctx.fillStyle = '#1aa3a8';
  ctx.fillRect(0, 0, 64, 64);
  ctx.fillStyle = 'rgba(255,255,255,0.06)';
  for (let y = 0; y < 64; y += 6) ctx.fillRect(0, y, 64, 1);
  ctx.fillStyle = 'rgba(0,0,0,0.10)';
  for (let x = 0; x < 64; x += 16) ctx.fillRect(x, 0, 1, 64);
  for (let y = 0; y < 64; y += 16) ctx.fillRect(0, y, 64, 1);

  const tex = new THREE.CanvasTexture(c);
  tex.colorSpace = THREE.SRGBColorSpace;
  tex.wrapS = THREE.RepeatWrapping;
  tex.wrapT = THREE.RepeatWrapping;
  tex.repeat.set(14, 14);
  tex.magFilter = THREE.NearestFilter;
  tex.minFilter = THREE.NearestFilter;
  return tex;
}

// External seamless normal map (provided by user)
const texLoader = new THREE.TextureLoader();

const waterTex = makeWaterTexture();
const waterNormal = texLoader.load('./assets/ocean_normal.png');
waterNormal.wrapS = THREE.RepeatWrapping;
waterNormal.wrapT = THREE.RepeatWrapping;
waterNormal.repeat.set(18, 18);
waterNormal.magFilter = THREE.NearestFilter;
waterNormal.minFilter = THREE.NearestFilter;
// Normal maps must stay in linear/no colorspace
waterNormal.colorSpace = THREE.NoColorSpace;
waterNormal.anisotropy = renderer.capabilities.getMaxAnisotropy();

const waterMat = new THREE.MeshPhysicalMaterial({
  color: 0x1aa3a8,
  map: waterTex,
  roughness: 0.22,
  metalness: 0.0,
  clearcoat: 1.0,
  clearcoatRoughness: 0.08,
  normalMap: waterNormal,
  normalScale: new THREE.Vector2(0.45, 0.45),
  flatShading: true,
});
const water = new THREE.Mesh(waterGeo, waterMat);
scene.add(water);

function waterHeight(x, z, t) {
  // coherent waves (visual)
  const a = 0.15;
  const w1 = Math.sin((x * 0.040) + (t * 0.75)) * 0.78;
  const w2 = Math.sin((z * 0.038) - (t * 0.68)) * 0.62;
  const w3 = Math.sin(((x + z) * 0.028) + (t * 1.05)) * 0.45;
  const h = a * (w1 + w2 + w3);
  const step = 0.035;
  return Math.round(h / step) * step;
}
function updateWaterMesh(t) {
  const pos = waterGeo.attributes.position;
  for (let i = 0; i < pos.count; i++) {
    const x = pos.getX(i);
    const z = pos.getZ(i);
    pos.setY(i, waterHeight(x, z, t));
  }
  pos.needsUpdate = true;
  waterGeo.computeVertexNormals();
}

// Islands (placeholder)
const islandGroup = new THREE.Group();
scene.add(islandGroup);
function addIsland(x, z, r=14){
  const base = new THREE.Mesh(
    new THREE.CylinderGeometry(r*1.3, r*2.1, r*0.7, 12, 1),
    new THREE.MeshStandardMaterial({ color: 0x243622, roughness: 1.0 })
  );
  base.position.set(x, r*0.25, z);
  const top = new THREE.Mesh(
    new THREE.ConeGeometry(r*1.15, r*0.95, 11, 1),
    new THREE.MeshStandardMaterial({ color: 0x2f4a2f, roughness: 1.0 })
  );
  top.position.set(x + r*0.12, r*0.78, z - r*0.1);
  islandGroup.add(base, top);
}
addIsland(90, 80, 18);
addIsland(-120, 40, 20);
addIsland(-60, -140, 16);

// Sail texture helper (pixel weave)
function makePixelSailTex() {
  const c = document.createElement('canvas');
  c.width = 32; c.height = 32;
  const ctx = c.getContext('2d');
  ctx.fillStyle = '#eaf2ff'; ctx.fillRect(0,0,32,32);
  ctx.fillStyle = '#c6d7f3';
  for (let y=0; y<32; y+=4) ctx.fillRect(0, y, 32, 1);
  ctx.fillStyle = '#a6bfe6';
  for (let x=0; x<32; x+=8) ctx.fillRect(x, 0, 1, 32);
  const tex = new THREE.CanvasTexture(c);
  tex.colorSpace = THREE.SRGBColorSpace;
  tex.magFilter = THREE.NearestFilter;
  tex.minFilter = THREE.NearestFilter;
  tex.wrapS = THREE.RepeatWrapping;
  tex.wrapT = THREE.RepeatWrapping;
  return tex;
}

// Boat model (Nordic Folkboat-inspired, procedural)
const boatGroup = new THREE.Group();
scene.add(boatGroup);

// Materials (folkboat palette)
const matHull = new THREE.MeshStandardMaterial({ color: 0xb11e2a, roughness: 0.78, metalness: 0.05 }); // classic red hull
const matHullStripe = new THREE.MeshStandardMaterial({ color: 0xf2f2f2, roughness: 0.55, metalness: 0.0 });
const matDeck = new THREE.MeshStandardMaterial({ color: 0xf3efe6, roughness: 0.92, metalness: 0.0 });
const matTeak = new THREE.MeshStandardMaterial({ color: 0x9b6b3f, roughness: 0.86, metalness: 0.0 });
const matCabin = new THREE.MeshStandardMaterial({ color: 0xf3efe6, roughness: 0.92, metalness: 0.0 });
const matMast = new THREE.MeshStandardMaterial({ color: 0x3a271b, roughness: 0.90, metalness: 0.05 });
const matHardware = new THREE.MeshStandardMaterial({ color: 0x2b2f3a, roughness: 0.65, metalness: 0.25 });
const matWindow = new THREE.MeshStandardMaterial({ color: 0x1a2a3f, roughness: 0.15, metalness: 0.2, envMapIntensity: 0.9 });

// Dimensions (world units are arbitrary but consistent)
const L = 7.0;     // length
const B = 1.55;    // beam
const H = 0.85;    // hull depth

// Hull: capsule scaled (smooth, boat-like) aligned along Z (bow +Z, stern -Z)
const hull = new THREE.Mesh(
  new THREE.CapsuleGeometry(0.78, 5.2, 10, 18),
  matHull
);
// Capsule is along Y. Rotate so length -> Z.
hull.geometry.rotateX(Math.PI / 2);
hull.scale.set(B * 0.52, H * 0.62, L * 0.16);
hull.position.set(0, 0.22, 0.05);
boatGroup.add(hull);

// Boot-top stripe (thin white line)
const stripe = new THREE.Mesh(
  new THREE.CylinderGeometry(0.82, 0.82, 5.15, 32, 1, true),
  matHullStripe
);
stripe.geometry.rotateX(Math.PI / 2);
stripe.scale.set(B * 0.50, 1.0, L * 0.155);
stripe.position.set(0, 0.45, 0.05);
boatGroup.add(stripe);

// Deck: extruded narrow shape with rounded bow
const deckShape = new THREE.Shape();
deckShape.moveTo(0.00, 3.35);
deckShape.quadraticCurveTo(0.55, 2.9, 0.75, 2.1);
deckShape.quadraticCurveTo(0.90, 0.5, 0.78, -2.4);
deckShape.lineTo(-0.78, -2.4);
deckShape.quadraticCurveTo(-0.90, 0.5, -0.75, 2.1);
deckShape.quadraticCurveTo(-0.55, 2.9, 0.00, 3.35);

const deckGeo = new THREE.ExtrudeGeometry(deckShape, {
  depth: 0.14,
  bevelEnabled: true,
  bevelThickness: 0.06,
  bevelSize: 0.06,
  bevelSegments: 2,
  steps: 1
});
deckGeo.rotateX(-Math.PI / 2);
deckGeo.translate(0, 0.60, 0.05);

const deck = new THREE.Mesh(deckGeo, matDeck);
boatGroup.add(deck);

// Teak toe-rail
const rail = new THREE.Mesh(
  new THREE.TorusGeometry(1.05, 0.03, 10, 40),
  matTeak
);
rail.scale.set(B * 0.52, 1.0, L * 0.26);
rail.rotation.x = Math.PI / 2;
rail.position.set(0, 0.73, 0.05);
boatGroup.add(rail);

// Cabin (low, classic)
const cabinBase = new THREE.Mesh(new THREE.BoxGeometry(1.05, 0.42, 1.85), matCabin);
cabinBase.position.set(0, 0.95, -0.75);
boatGroup.add(cabinBase);

const cabinRoof = new THREE.Mesh(new THREE.BoxGeometry(0.95, 0.22, 1.65), matTeak);
cabinRoof.position.set(0, 1.22, -0.75);
boatGroup.add(cabinRoof);

// Cabin windows (port/starboard)
function makeWindow(x, y, z, rotY) {
  const w = new THREE.Mesh(new THREE.PlaneGeometry(0.42, 0.18), matWindow);
  w.position.set(x, y, z);
  w.rotation.y = rotY;
  boatGroup.add(w);
}
makeWindow(0.53, 1.00, -0.95, -Math.PI / 2);
makeWindow(0.53, 1.00, -0.55, -Math.PI / 2);
makeWindow(-0.53, 1.00, -0.95, Math.PI / 2);
makeWindow(-0.53, 1.00, -0.55, Math.PI / 2);

// Cockpit (simple recess + coaming)
const cockpit = new THREE.Mesh(new THREE.BoxGeometry(1.05, 0.18, 1.55), new THREE.MeshStandardMaterial({ color: 0x2a2d35, roughness: 0.9 }));
cockpit.position.set(0, 0.78, -1.55);
boatGroup.add(cockpit);

const coaming = new THREE.Mesh(new THREE.BoxGeometry(1.20, 0.22, 1.65), matTeak);
coaming.position.set(0, 0.92, -1.55);
boatGroup.add(coaming);

// Keel (fin) – visual
const keel = new THREE.Mesh(new THREE.BoxGeometry(0.16, 0.75, 1.20), matHardware);
keel.position.set(0, -0.10, -0.20);
boatGroup.add(keel);

// Bow marker (front clarity)
const bowMark = new THREE.Mesh(new THREE.ConeGeometry(0.12, 0.28, 12), new THREE.MeshStandardMaterial({ color: 0xffe2b8, roughness: 0.55 }));
bowMark.position.set(0, 0.80, 3.55);
bowMark.rotation.x = Math.PI;
boatGroup.add(bowMark);

// Mast (slightly forward)
const mast = new THREE.Mesh(new THREE.CylinderGeometry(0.05, 0.07, 6.2, 12), matMast);
mast.position.set(0, 3.35, 0.45);
boatGroup.add(mast);

// Forestay (visual cable)
const stay = new THREE.Mesh(new THREE.CylinderGeometry(0.012, 0.012, 4.2, 6), matHardware);
stay.position.set(0, 3.15, 2.05);
stay.rotation.x = -0.40;
boatGroup.add(stay);

// Main sail pivot at mast (boom goes aft)
const sailPivot = new THREE.Group();
sailPivot.position.set(0, 3.05, 0.45);
boatGroup.add(sailPivot);

// Sail texture (reuse)
const sailTex = makePixelSailTex();
const sailMat = new THREE.MeshStandardMaterial({ map: sailTex, side: THREE.DoubleSide, roughness: 0.88, metalness: 0.0 });

// Main sail (explicit triangle in YZ plane; foot goes aft -Z, luff at mast)
const mainGeo = new THREE.BufferGeometry();
const mainVerts = new Float32Array([
  // tack (at mast/boom)
  0.0, 0.0,  0.0,
  // head (up the mast)
  0.0, 4.10, 0.0,
  // clew (aft along boom)
  0.0, 0.0, -2.70,
]);
mainGeo.setAttribute('position', new THREE.BufferAttribute(mainVerts, 3));
mainGeo.setIndex([0, 1, 2]);
mainGeo.computeVertexNormals();
const mainSail = new THREE.Mesh(mainGeo, sailMat);
mainSail.position.set(0.0, -0.10, 0.0);
sailPivot.add(mainSail);
// Alias for physics visuals
const sail = mainSail;

// Boom along -Z (aft)
const boom = new THREE.Mesh(new THREE.CylinderGeometry(0.035, 0.04, 3.1, 10), matMast);
boom.rotation.x = -Math.PI / 2;
boom.position.set(0.0, -0.10, -1.55);
sailPivot.add(boom);

// Jib sail (small triangle near bow, fixed to same trim for now)
const jibPivot = new THREE.Group();
jibPivot.position.set(0, 2.55, 2.25);
boatGroup.add(jibPivot);

const jibShape = new THREE.Shape();
jibShape.moveTo(0, 0);
jibShape.lineTo(0, 2.75);
jibShape.lineTo(1.55, 0);
jibShape.lineTo(0, 0);
const jibGeo = new THREE.ShapeGeometry(jibShape, 12);
jibGeo.translate(0.0, -0.06, 0.0);
const jib = new THREE.Mesh(jibGeo, sailMat);
// Foot goes aft (-Z) as well
jib.rotation.y = -Math.PI / 2;
jibPivot.add(jib);

// Rudder visual at stern (outside)
const rudderVis = new THREE.Mesh(new THREE.BoxGeometry(0.08, 0.45, 0.40), matHardware);
rudderVis.position.set(0, 0.22, -3.45);
boatGroup.add(rudderVis);

// Streamer (apparent wind vane)
const streamer = new THREE.Mesh(
  new THREE.PlaneGeometry(1.0, 0.22, 1, 1),
  new THREE.MeshStandardMaterial({ color: 0xff2b55, side: THREE.DoubleSide, roughness: 0.7 })
);
streamer.position.set(0.18, 4.95, 0.45);
streamer.rotation.y = Math.PI / 2;
boatGroup.add(streamer);

// --- end boat model ---

// Wind particles
const particleCount = 520;
const particleGeo = new THREE.BufferGeometry();
const particlePos = new Float32Array(particleCount * 3);
const particleVel = new Float32Array(particleCount * 3);
particleGeo.setAttribute('position', new THREE.BufferAttribute(particlePos, 3));
const particleMat = new THREE.PointsMaterial({ color: 0xffffff, size: 0.06, sizeAttenuation: true, transparent: true, opacity: 0.22 });
const windParticles = new THREE.Points(particleGeo, particleMat);
scene.add(windParticles);

// Wake foam (pixel boxes)
const foamGroup = new THREE.Group();
scene.add(foamGroup);
const foamPool = [];
const foamMax = 140;
const foamGeo = new THREE.BoxGeometry(0.20, 0.06, 0.20);
function spawnFoam(p, v, strength) {
  let f = foamPool.find(x => !x.alive);
  if (!f) {
    if (foamPool.length >= foamMax) return;
    const mesh = new THREE.Mesh(foamGeo, new THREE.MeshBasicMaterial({ color: 0xffffff, transparent: true, opacity: 0.0 }));
    foamGroup.add(mesh);
    f = { mesh, alive: false, life: 0, max: 0, v: new THREE.Vector3() };
    foamPool.push(f);
  }
  f.alive = true;
  f.life = 0;
  f.max = 0.55 + 0.65 * strength;
  f.mesh.position.copy(p);
  f.v.copy(v);
  f.mesh.scale.setScalar(1.0);
  f.mesh.material.opacity = 0.50 * strength;
}

// Boat physics state (XZ plane)
const boat = {
  pos: new THREE.Vector3(0, 0, 0),
  vel: new THREE.Vector3(0, 0, 0),
  heading: 0.0,     // yaw radians, 0 -> +Z
  yawRate: 0.0,     // rad/s
  rudder: 0.0,      // rad (positive starboard)
  sail: THREE.MathUtils.degToRad(55), // rad (positive to starboard, relative to centerline)
  sailDeploy: 1.0, // 0..1 (furl/unfurl)
  engineOn: false,
  throttle: 0.0, // 0..1
  mass: 520,
  Iz: 980,
};

// Wind (direction is where the air moves toward, in world XZ)
const wind = { dir: THREE.MathUtils.degToRad(30), speed: 7.2 };

function trueWindVec(t) {
  // coherent gusting
  const gust = 1.0 + 0.15 * Math.sin(t * 0.55) + 0.10 * Math.sin(t * 1.7);
  const s = wind.speed * gust;
  return new THREE.Vector3(Math.sin(wind.dir) * s, 0, Math.cos(wind.dir) * s);
}

// Input
const input = {
  left:false, right:false, sailOut:false, sailIn:false,
  furlIn:false, furlOut:false,
  engineToggle:false,
  throttleUp:false, throttleDown:false,
  zoomIn:false, zoomOut:false,
  dragging:false, mx:0, my:0,
  camYaw: 0.0, camPitch: 0.15, camDist: 15.0
};

window.addEventListener('keydown', (e) => {
  if (e.code === 'KeyA' || e.code === 'ArrowLeft') input.left = true;
  if (e.code === 'KeyD' || e.code === 'ArrowRight') input.right = true;
  if (e.code === 'KeyQ') input.sailOut = true;
  if (e.code === 'KeyE') input.sailIn = true;
  if (e.code === 'KeyZ') input.furlIn = true;
  if (e.code === 'KeyX') input.furlOut = true;
  if (e.code === 'KeyT') input.throttleUp = true;
  if (e.code === 'KeyG') input.throttleDown = true;
  if (e.code === 'KeyM') boat.engineOn = !boat.engineOn;
  if (e.code === 'KeyW') input.zoomIn = true;
  if (e.code === 'KeyS') input.zoomOut = true;
  if (e.code === 'KeyR') reset();
  if (e.code === 'KeyH') showHud = !showHud;
});
window.addEventListener('keyup', (e) => {
  if (e.code === 'KeyA' || e.code === 'ArrowLeft') input.left = false;
  if (e.code === 'KeyD' || e.code === 'ArrowRight') input.right = false;
  if (e.code === 'KeyQ') input.sailOut = false;
  if (e.code === 'KeyE') input.sailIn = false;
  if (e.code === 'KeyZ') input.furlIn = false;
  if (e.code === 'KeyX') input.furlOut = false;
  if (e.code === 'KeyT') input.throttleUp = false;
  if (e.code === 'KeyG') input.throttleDown = false;
  if (e.code === 'KeyW') input.zoomIn = false;
  if (e.code === 'KeyS') input.zoomOut = false;
});
renderer.domElement.addEventListener('mousedown', (e) => { input.dragging = true; input.mx = e.clientX; input.my = e.clientY; });
window.addEventListener('mouseup', () => input.dragging = false);
window.addEventListener('mousemove', (e) => {
  if (!input.dragging) return;
  const dx = e.clientX - input.mx;
  const dy = e.clientY - input.my;
  input.mx = e.clientX; input.my = e.clientY;
  input.camYaw -= dx * 0.004;
  input.camPitch = clamp(input.camPitch - dy * 0.0025, -0.05, 0.75);
});

function reset() {
  boat.pos.set(0,0,0);
  boat.vel.set(0,0,0);
  boat.heading = 0;
  boat.yawRate = 0;
  boat.rudder = 0;
  boat.sail = THREE.MathUtils.degToRad(25);
}

// Coordinate helpers
function basisFromHeading(h) {
  // forward +Z when h=0; right +X
  const fwd = new THREE.Vector3(Math.sin(h), 0, Math.cos(h));
  const right = new THREE.Vector3(Math.cos(h), 0, -Math.sin(h));
  return { fwd, right };
}

// Core physics step (rebuilt)
function stepBoat(dt, t) {
  // Control surfaces (rate-limited + auto-center)
  const rudderMax = THREE.MathUtils.degToRad(28);
  const rudderRate = THREE.MathUtils.degToRad(85);
  const rudderReturn = THREE.MathUtils.degToRad(60);

  const rudInput = (input.left ? 1 : 0) - (input.right ? 1 : 0);
  if (rudInput !== 0) boat.rudder += rudInput * rudderRate * dt;
  else {
    const sign = Math.sign(boat.rudder);
    const mag = Math.abs(boat.rudder);
    boat.rudder = sign * Math.max(0, mag - rudderReturn * dt);
  }
  boat.rudder = clamp(boat.rudder, -rudderMax, rudderMax);

  const sailRate = THREE.MathUtils.degToRad(75);
  if (input.sailOut) boat.sail += sailRate * dt;
  if (input.sailIn) boat.sail -= sailRate * dt;
  boat.sail = clamp(boat.sail, -Math.PI * 0.5, Math.PI * 0.5);

  // Sail deploy (furl/unfurl)
  const furlRate = 0.85;
  if (input.furlIn) boat.sailDeploy = clamp(boat.sailDeploy - furlRate * dt, 0.0, 1.0);
  if (input.furlOut) boat.sailDeploy = clamp(boat.sailDeploy + furlRate * dt, 0.0, 1.0);

  // Engine throttle
  const thrRate = 0.65;
  if (input.throttleUp) boat.throttle = clamp(boat.throttle + thrRate * dt, 0.0, 1.0);
  if (input.throttleDown) boat.throttle = clamp(boat.throttle - thrRate * dt, 0.0, 1.0);
  if (!boat.engineOn) boat.throttle = Math.max(0.0, boat.throttle - 0.9 * dt);

  // Basis and wind
  const { fwd, right } = basisFromHeading(boat.heading);
  const tw = trueWindVec(t);

  // Wind vectors (tw is AIR-TO; we use FROM for sailing logic)
  const aw = tw.clone().sub(boat.vel);          // air-to relative boat
  const twFrom = tw.clone().multiplyScalar(-1);
  const appFrom = twFrom.clone().sub(boat.vel); // apparent wind FROM
  const appSp = Math.max(appFrom.length(), 0.001);

  // Angles in boat frame (FROM)
  const twF = twFrom.dot(fwd);
  const twR = twFrom.dot(right);
  const twa = Math.atan2(twR, twF);            // + = from starboard
  const absTWA = Math.abs(twa);

  // Point-of-sail buckets (simple if/else "polar" model)
  // These are tunable targets for sail angle magnitude (deg) and base power.
  let targetSail = 20;
  let basePower = 0.75;

  const d = THREE.MathUtils.radToDeg(absTWA);

  if (d < 35) {               // no-go
    targetSail = 10;
    basePower = 0.05;
  } else if (d < 60) {        // close-hauled (orsa)
    targetSail = 20;
    basePower = 0.85;
  } else if (d < 90) {        // close reach (dar apaz)
    targetSail = 35;
    basePower = 0.95;
  } else if (d < 120) {       // beam reach (apaz)
    targetSail = 55;
    basePower = 1.05;
  } else if (d < 150) {       // broad reach (geniş apaz)
    targetSail = 75;
    basePower = 0.95;
  } else {                    // run (pupa)
    targetSail = 85;
    basePower = 0.75;
  }

  // Trim efficiency: how close the sail angle is to the target for this point-of-sail
  const sailAbs = Math.abs(THREE.MathUtils.radToDeg(boat.sail));
  const trimWindow = (d < 60) ? 18 : (d < 120 ? 22 : 26);
  const trimEff = clamp(1.0 - Math.abs(sailAbs - targetSail) / trimWindow, 0.0, 1.0);

  // Apparent wind intensity factor (more speed into wind -> more apparent wind -> more power)
  // Keep it sim-cade stable.
  const q = appSp * appSp;

  // Sail force from apparent wind (lift + drag), with soft stall.
  const appF = appFrom.dot(fwd);
  const appR = appFrom.dot(right);
  const appAng = Math.atan2(appR, appF);
  const aoa = wrapAngle(appAng - boat.sail);
  const absAoa = Math.abs(aoa);
  const stall = clamp(1 - (absAoa / THREE.MathUtils.degToRad(80)), 0.0, 1.0);
  const liftCoeff = Math.sin(2 * absAoa) * stall;
  const dragCoeff = 0.20 + 1.60 * Math.pow(Math.sin(absAoa), 2);

  const appDir = appFrom.clone().multiplyScalar(1 / appSp);
  const dragDir = appDir.clone().multiplyScalar(-1);
  const baseLift = new THREE.Vector3()
    .addScaledVector(fwd, -appR / appSp)
    .addScaledVector(right, appF / appSp);
  const liftDir = baseLift.multiplyScalar(Math.sign(aoa || 1));

  const sailPower = 1.35 * basePower * trimEff * boat.sailDeploy;
  const F_sail = new THREE.Vector3()
    .addScaledVector(dragDir, q * dragCoeff * sailPower)
    .addScaledVector(liftDir, q * liftCoeff * sailPower);

  const jibTarget = clamp(targetSail * 0.65, 12, 60);
  const jibSign = Math.sign(twa || 1);
  const jibTrim = clamp(THREE.MathUtils.degToRad(jibTarget) * jibSign, -Math.PI * 0.4, Math.PI * 0.4);
  const jibAoa = wrapAngle(appAng - jibTrim);
  const jibAbsAoa = Math.abs(jibAoa);
  const jibStall = clamp(1 - (jibAbsAoa / THREE.MathUtils.degToRad(85)), 0.0, 1.0);
  const jibLift = Math.sin(2 * jibAbsAoa) * jibStall;
  const jibDrag = 0.24 + 1.45 * Math.pow(Math.sin(jibAbsAoa), 2);
  const jibLiftDir = baseLift.clone().multiplyScalar(Math.sign(jibAoa || 1));
  const jibPower = sailPower * 0.55;
  const F_jib = new THREE.Vector3()
    .addScaledVector(dragDir, q * jibDrag * jibPower)
    .addScaledVector(jibLiftDir, q * jibLift * jibPower);

  // Engine thrust (motorlu seyir): forward push independent of wind
  const enginePower = 320.0; // N-ish (sim-cade)
  const F_engine = boat.engineOn ? fwd.clone().multiplyScalar(enginePower * boat.throttle) : new THREE.Vector3();

  // Hydrodynamic resistance in boat frame (keel effect via strong lateral damping)
  const vF = boat.vel.dot(fwd);
  const vR = boat.vel.dot(right);

  const engineGrip = boat.engineOn ? (1.0 + boat.throttle * 0.85) : 1.0;
  const speedF = Math.abs(vF);
  const kF_lin = 18.0, kF_quad = 10.5;   // forward drag
  const kR_lin = (140.0 + speedF * 8.0) * engineGrip;
  const kR_quad = (85.0 + speedF * 6.0) * engineGrip;  // lateral (keel) drag

  const F_hydro = new THREE.Vector3()
    .addScaledVector(fwd,  -(kF_lin * vF + kF_quad * vF * Math.abs(vF)))
    .addScaledVector(right,-(kR_lin * vR + kR_quad * vR * Math.abs(vR)));

  // Rudder: yaw moment dominated, small sideforce (turning)
  const flowWater = Math.abs(vF);
  const rudSide = boat.rudder * flowWater * flowWater * 20.0;
  const F_rudder = right.clone().multiplyScalar(rudSide);

  // Total force
  const F = new THREE.Vector3().add(F_sail).add(F_jib).add(F_engine).add(F_hydro).add(F_rudder);

  // Integrate linear
  const acc = F.multiplyScalar(1 / boat.mass);
  boat.vel.addScaledVector(acc, dt);
  boat.pos.addScaledVector(boat.vel, dt);

  // Yaw dynamics
  const lever = 2.55;
  let torque = boat.rudder * flowWater * flowWater * 420.0;
  torque += (F_sail.dot(right)) * 0.10;              // weather-helm-ish
  torque += -vR * (12.0 + 8.0 * engineGrip);
  if (boat.vel.lengthSq() > 0.05) {
    const velHeading = Math.atan2(boat.vel.x, boat.vel.z);
    const slipAngle = wrapAngle(velHeading - boat.heading);
    torque += -slipAngle * (45.0 + flowWater * 22.0);
  }
  torque += -boat.yawRate * (3.4 + flowWater * 0.22);

  const yawAcc = torque / boat.Iz;
  boat.yawRate += yawAcc * dt;
  boat.yawRate = clamp(boat.yawRate, -0.70, 0.70);
  boat.yawRate *= Math.pow(0.08, dt);
  boat.heading = wrapAngle(boat.heading + boat.yawRate * dt);

  // Visual: sail follows trim (negative sign keeps "positive trim" intuitive)
  sailPivot.rotation.y = -boat.sail;
  jibPivot.rotation.y = -jibTrim;

  // Visual: luffing when badly trimmed (trimEff low) in upwind/reach
  stepBoat._flap = (stepBoat._flap || 0) + dt * (6.0 + appSp * 0.5);
  const luff = clamp(1.0 - trimEff, 0.0, 1.0) * (d < 120 ? 1.0 : 0.5);
  sail.rotation.x = 0.0;
  const luffSign = Math.sign(twa || 1);
  sail.rotation.z = luff * 0.12 * Math.sin(stepBoat._flap * 7.0) * luffSign;

  // Streamer: align to apparent wind FROM in boat frame
  streamer.rotation.y = appAng + Math.PI * 0.5;

  // Rudder visual
  rudderVis.rotation.y = boat.rudder;

  // For HUD: alpha is "trim error" proxy now
  const alpha = THREE.MathUtils.degToRad(Math.abs(sailAbs - targetSail));

  const windEff = clamp(basePower * trimEff * boat.sailDeploy, 0.0, 1.0);
  const heel = clamp((-appR / appSp) * (0.10 + windEff * 0.18), -0.22, 0.22);

  return { tw, aw, flow: appFrom, speed: boat.vel.length(), alpha, twa, flowAng: appAng, fwd, right, posEff: windEff, sailDeploy: boat.sailDeploy, engineOn: boat.engineOn, throttle: boat.throttle, heel, windEff };
}

// Camera follow
function updateCamera(dt) {
  if (input.zoomIn) input.camDist = clamp(input.camDist - 9.0 * dt, 8.0, 26.0);
  if (input.zoomOut) input.camDist = clamp(input.camDist + 9.0 * dt, 8.0, 26.0);

  const target = boatGroup.position.clone().add(new THREE.Vector3(0, 2.0, 0));
  const yaw = input.camYaw + boat.heading + Math.PI; // behind boat
  const pitch = input.camPitch;

  const cp = Math.cos(pitch), sp = Math.sin(pitch);
  const sy = Math.sin(yaw), cy = Math.cos(yaw);

  const offset = new THREE.Vector3(sy * cp, sp, cy * cp).multiplyScalar(input.camDist);
  camera.position.copy(target).add(offset);
  camera.lookAt(target);
}

// HUD + compass
function updateBottomHud(info) {
  const hdgDeg = (THREE.MathUtils.radToDeg(boat.heading) + 360) % 360;
  const rudDeg = THREE.MathUtils.radToDeg(boat.rudder);
  const sailDeg = THREE.MathUtils.radToDeg(boat.sail);

  hudEls.speedValue.textContent = `${info.speed.toFixed(2)} m/s`;
  hudEls.headingValue.textContent = `HDG ${String(Math.round(hdgDeg)).padStart(3,'0')}°`;
  hudEls.rudderValue.textContent = `${rudDeg.toFixed(0)}°`;
  hudEls.sailValue.textContent = `${sailDeg.toFixed(0)}°`;
  hudEls.trueWindValue.textContent = `${info.tw.length().toFixed(1)} m/s`;
  hudEls.appWindValue.textContent = `${info.flow.length().toFixed(1)} m/s`;

  const rudRot = clamp(rudDeg, -90, 90);
  const sailRot = clamp(sailDeg, -90, 90);
  hudEls.rudderNeedle.style.transform = `translate(-50%, -95%) rotate(${rudRot}deg)`;
  hudEls.sailNeedle.style.transform = `translate(-50%, -95%) rotate(${sailRot}deg)`;

  // relative wind angles: in our convention use atan2(x, z)
  const twFrom = info.tw.clone().multiplyScalar(-1);
  const relTrue = wrapAngle(Math.atan2(twFrom.x, twFrom.z) - boat.heading);
  const relApp  = wrapAngle(Math.atan2(info.flow.x, info.flow.z) - boat.heading);
  hudEls.trueWindArrow.style.transform = `rotate(${THREE.MathUtils.radToDeg(relTrue)}deg)`;
  hudEls.appWindArrow.style.transform  = `rotate(${THREE.MathUtils.radToDeg(relApp)}deg)`;

  // Sail + engine UI
  if (hudEls.sailStateValue) {
    const pct = Math.round((info.sailDeploy ?? 1) * 100);
    hudEls.sailStateValue.textContent = `${pct}%`;
    if (hudEls.sailFill) hudEls.sailFill.style.width = `${pct}%`;
  }
  if (hudEls.engineValue) {
    hudEls.engineValue.textContent = (info.engineOn ? `ON ${Math.round((info.throttle ?? 0)*100)}%` : 'OFF');
    if (hudEls.throttleFill) hudEls.throttleFill.style.width = `${Math.round((info.throttle ?? 0)*100)}%`;
  }
  if (hudEls.windEffValue) {
    const pct = Math.round((info.windEff ?? 0) * 100);
    hudEls.windEffValue.textContent = `${pct}%`;
    if (hudEls.windEffFill) hudEls.windEffFill.style.width = `${pct}%`;
  }
}

function drawCompass(info) {
  if (!showHud) return;

  const dpr = window.devicePixelRatio || 1;
  const w = canvasHud.width / dpr;
  const h = canvasHud.height / dpr;
  hudCtx.clearRect(0,0,w,h);

  const cx = w - 140;
  const cy = 140;
  const r = 86;

  hudCtx.globalAlpha = 0.75;
  hudCtx.fillStyle = '#0a101c';
  hudCtx.strokeStyle = 'rgba(255,255,255,0.12)';
  roundRect(hudCtx, cx - 120, cy - 120, 240, 240, 16, true, true);
  hudCtx.globalAlpha = 1.0;

  hudCtx.lineWidth = 2;
  hudCtx.strokeStyle = 'rgba(255,255,255,0.18)';
  hudCtx.beginPath();
  hudCtx.arc(cx, cy, r, 0, Math.PI*2);
  hudCtx.stroke();

  hudCtx.fillStyle = 'rgba(255,255,255,0.70)';
  hudCtx.font = '12px ui-sans-serif, system-ui';
  hudCtx.textAlign = 'center';
  hudCtx.textBaseline = 'middle';
  hudCtx.fillText('N', cx, cy - r - 12);
  hudCtx.fillText('S', cx, cy + r + 12);
  hudCtx.fillText('W', cx - r - 12, cy);
  hudCtx.fillText('E', cx + r + 12, cy);

  // boat heading indicator (up)
  hudCtx.strokeStyle = 'rgba(255,255,255,0.28)';
  hudCtx.beginPath();
  hudCtx.moveTo(cx, cy);
  hudCtx.lineTo(cx, cy - r + 6);
  hudCtx.stroke();

  const twFrom = info.tw.clone().multiplyScalar(-1);
  const relTrue = wrapAngle(Math.atan2(twFrom.x, twFrom.z) - boat.heading);
  const relApp  = wrapAngle(Math.atan2(info.flow.x, info.flow.z) - boat.heading);
  drawArrow(cx, cy, r - 10, relTrue, 'rgba(120, 200, 255, 0.95)', 3);
  drawArrow(cx, cy, r - 40, relApp,  'rgba(255, 170, 120, 0.95)', 3);

  hudCtx.textAlign = 'left';
  hudCtx.fillStyle = 'rgba(255,255,255,0.85)';
  hudCtx.font = '12px ui-sans-serif, system-ui';

  const baseX = cx - 104;
  let y = cy + 76;
  hudCtx.fillText(`Boat speed: ${info.speed.toFixed(2)} m/s`, baseX, y); y += 16;
  hudCtx.fillText(`TWA: ${Math.abs(THREE.MathUtils.radToDeg(info.twa)).toFixed(0)}°`, baseX, y); y += 16;
  hudCtx.fillText(`AOA: ${Math.abs(THREE.MathUtils.radToDeg(info.alpha)).toFixed(0)}°`, baseX, y); y += 16;
  hudCtx.fillText(`Drive: ${(info.posEff*100).toFixed(0)}%`, baseX, y);

  function drawArrow(cx, cy, len, ang, color, lw) {
    const a = ang - Math.PI/2;
    const x2 = cx + Math.cos(a) * len;
    const y2 = cy + Math.sin(a) * len;

    hudCtx.strokeStyle = color;
    hudCtx.lineWidth = lw;
    hudCtx.beginPath();
    hudCtx.moveTo(cx, cy);
    hudCtx.lineTo(x2, y2);
    hudCtx.stroke();

    const head = 10;
    const left = a + Math.PI * 0.8;
    const right = a - Math.PI * 0.8;

    hudCtx.beginPath();
    hudCtx.moveTo(x2, y2);
    hudCtx.lineTo(x2 + Math.cos(left) * head, y2 + Math.sin(left) * head);
    hudCtx.lineTo(x2 + Math.cos(right) * head, y2 + Math.sin(right) * head);
    hudCtx.closePath();
    hudCtx.fillStyle = color;
    hudCtx.fill();
  }
}

function roundRect(ctx, x, y, w, h, r, fill, stroke) {
  if (typeof r === 'number') r = {tl:r,tr:r,br:r,bl:r};
  ctx.beginPath();
  ctx.moveTo(x + r.tl, y);
  ctx.lineTo(x + w - r.tr, y);
  ctx.quadraticCurveTo(x + w, y, x + w, y + r.tr);
  ctx.lineTo(x + w, y + h - r.br);
  ctx.quadraticCurveTo(x + w, y + h, x + w - r.br, y + h);
  ctx.lineTo(x + r.bl, y + h);
  ctx.quadraticCurveTo(x, y + h, x, y + h - r.bl);
  ctx.lineTo(x, y + r.tl);
  ctx.quadraticCurveTo(x, y, x + r.tl, y);
  ctx.closePath();
  if (fill) ctx.fill();
  if (stroke) ctx.stroke();
}

// Pixelated rendering
function resize() {
  const w = window.innerWidth;
  const h = window.innerHeight;

  const iw = Math.max(360, Math.floor(w * params.internalScale));
  const ih = Math.max(200, Math.floor(h * params.internalScale));

  renderer.setSize(iw, ih, false);
  renderer.domElement.style.width = w + 'px';
  renderer.domElement.style.height = h + 'px';
  renderer.domElement.style.imageRendering = 'pixelated';

  camera.aspect = w / h;
  camera.updateProjectionMatrix();

  canvasHud.width = w * devicePixelRatio;
  canvasHud.height = h * devicePixelRatio;
  canvasHud.style.width = w + 'px';
  canvasHud.style.height = h + 'px';
  hudCtx.setTransform(devicePixelRatio, 0, 0, devicePixelRatio, 0, 0);
}
window.addEventListener('resize', resize);
resize();

let last = performance.now() / 1000;

function animate() {
  requestAnimationFrame(animate);
  const now = performance.now() / 1000;
  const dt = clamp(now - last, 0.0, 0.033);
  last = now;

  updateWaterMesh(now);
  waterTex.offset.x = (now * 0.003) % 1;
  waterTex.offset.y = (now * 0.002) % 1;
  waterNormal.offset.x = (now * 0.006) % 1;
  waterNormal.offset.y = (now * 0.004) % 1;

  const info = stepBoat(dt, now);

  // Place boat on waves; apply gentle roll/pitch from wave slope + lateral speed
  const y = waterHeight(boat.pos.x, boat.pos.z, now);
  boatGroup.position.set(boat.pos.x, y + 0.30, boat.pos.z);
  boatGroup.rotation.y = boat.heading;

  boatKey.position.copy(boatGroup.position).add(new THREE.Vector3(0, 4.2, 0));

  // roll/pitch
  const { fwd, right } = basisFromHeading(boat.heading);
  const vR = boat.vel.dot(right);
  const vF = boat.vel.dot(fwd);

  const rollWave = (waterHeight(boat.pos.x + 1.3, boat.pos.z, now) - waterHeight(boat.pos.x - 1.3, boat.pos.z, now));
  const pitchWave = (waterHeight(boat.pos.x, boat.pos.z + 1.8, now) - waterHeight(boat.pos.x, boat.pos.z - 1.8, now));

  const targetRoll = clamp((-vR * 0.04) + rollWave * 0.70 + info.heel, -0.22, 0.22);
  const targetPitch = clamp((-vF * 0.014) + pitchWave * 0.55, -0.12, 0.12);
  const smooth = 1.0 - Math.pow(0.0012, dt);
  boatGroup.rotation.z = THREE.MathUtils.lerp(boatGroup.rotation.z, targetRoll, smooth);
  boatGroup.rotation.x = THREE.MathUtils.lerp(boatGroup.rotation.x, targetPitch, smooth);

  // True wind arrow in world
  const twN = info.tw.clone().multiplyScalar(-1);
  const twLen = Math.max(0.001, twN.length());
  twN.multiplyScalar(1 / twLen);
  windArrow.position.copy(boatGroup.position).add(new THREE.Vector3(0, 2.8, 0));
  windArrow.setDirection(twN);
  windArrow.setLength(3.0 + clamp(twLen, 0, 12) * 0.23);

  // Wind particles update (wrap around boat)
  const center = boatGroup.position;
  const span = 28;
  const pos = particleGeo.attributes.position;
  const tw = info.tw;
  for (let i = 0; i < particleCount; i++) {
    const idx = i * 3;
    let px = pos.array[idx + 0];
    let py = pos.array[idx + 1];
    let pz = pos.array[idx + 2];

    if (last === 0) {
      px = center.x + (Math.random()*2-1)*span;
      py = 0.8 + Math.random()*3.0;
      pz = center.z + (Math.random()*2-1)*span;
    }

    // init velocity if empty
    if (particleVel[idx+0] === 0 && particleVel[idx+2] === 0) {
      const j = 0.55;
      particleVel[idx+0] = tw.x + (Math.random()*2-1)*j;
      particleVel[idx+1] = (Math.random()*2-1)*0.15;
      particleVel[idx+2] = tw.z + (Math.random()*2-1)*j;
    }

    px += particleVel[idx+0] * dt * 0.55;
    py += particleVel[idx+1] * dt * 0.55;
    pz += particleVel[idx+2] * dt * 0.55;

    const dx = px - center.x;
    const dz = pz - center.z;
    if (Math.abs(dx) > span || Math.abs(dz) > span || py < 0.4 || py > 5.0) {
      const j = 0.55;
      px = center.x + (Math.random()*2-1)*span;
      py = 0.8 + Math.random()*3.0;
      pz = center.z + (Math.random()*2-1)*span;
      particleVel[idx+0] = tw.x + (Math.random()*2-1)*j;
      particleVel[idx+1] = (Math.random()*2-1)*0.15;
      particleVel[idx+2] = tw.z + (Math.random()*2-1)*j;
    }

    pos.array[idx+0] = px;
    pos.array[idx+1] = py;
    pos.array[idx+2] = pz;
  }
  pos.needsUpdate = true;

  // Wake foam
  const sternPos = boatGroup.position.clone().add(fwd.clone().multiplyScalar(-3.6)).add(new THREE.Vector3(0, 0.05, 0));
  const wakeStrength = clamp(info.speed / 5.2, 0, 1);
  if (wakeStrength > 0.05) {
    const rate = 16 + wakeStrength * 30;
    animate._foamAcc = (animate._foamAcc || 0) + dt * rate;
    while (animate._foamAcc > 1.0) {
      animate._foamAcc -= 1.0;
      const jitter = right.clone().multiplyScalar((Math.random()*2-1) * 0.48);
      const p = sternPos.clone().add(jitter);
      const v = boat.vel.clone().multiplyScalar(0.18).add(tw.clone().multiplyScalar(0.02));
      spawnFoam(p, v, wakeStrength);
    }
  }
  for (const f of foamPool) {
    if (!f.alive) continue;
    f.life += dt;
    const k = f.life / f.max;
    if (k >= 1) { f.alive = false; f.mesh.material.opacity = 0.0; continue; }
    f.mesh.position.addScaledVector(f.v, dt);
    f.v.multiplyScalar(Math.pow(0.06, dt));
    f.mesh.material.opacity = (1 - k) * 0.52 * wakeStrength;
    const s = 1.0 + k * 1.7;
    f.mesh.scale.setScalar(s);
  }

  updateCamera(dt);
  updateBottomHud(info);
  drawCompass(info);

  renderer.render(scene, camera);
}
animate();
