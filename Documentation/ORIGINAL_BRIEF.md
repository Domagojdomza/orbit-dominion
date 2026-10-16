```
# ORBIT DOMINION — UNITY 3D SPACE EXPLORATION, CIVILIZATION & CONQUEST PROTOTYPE

You are acting as a senior Unity gameplay engineer, technical artist, game architect, and game designer.

Build a polished playable prototype of a 3D space exploration, planet discovery, colonization, civilization-building, defense, and conquest game.

The prototype must prove one central fantasy:

"The player personally pilots a spaceship through an unknown galaxy, discovers new worlds, scans them, colonizes or conquers them, develops visible civilizations on their surfaces and in orbit, protects those worlds, and gradually builds an interplanetary empire."

The long-term concept combines:

- personal spaceship exploration,
- discovery of unknown planets and star systems,
- planetary scanning,
- colonization,
- visible civilization growth,
- resource production,
- orbital infrastructure,
- planetary defense,
- AI civilizations,
- territorial conquest,
- and eventually multiplayer competition for control of the galaxy.

The feeling of space exploration and discovering planets may take inspiration from the general exploration fantasy of Spore's Space Stage:

- personally traveling between worlds,
- seeing unknown planets in the distance,
- approaching them,
- discovering what they contain,
- interacting with civilizations,
- colonizing worlds,
- and gradually expanding across the galaxy.

Do NOT copy Spore's art, UI, assets, names, layouts, exact mechanics, audio, or protected creative expression.

This should instead feel like a modern, visually impressive interpretation of the fantasy:

"I have a spaceship. There is something unknown around that star. I want to fly there and see what it is."

Exploration and visual presentation are as important as strategy.

Do NOT turn this into a menu-driven strategy game where the spaceship is effectively a cursor.

The player must actually travel through space.


==================================================
1. FIRST ACTIONS
==================================================

Before changing files:

1. Inspect the repository.
2. Read any existing AGENTS.md.
3. Detect installed Unity versions and available tooling.
4. Preserve existing useful work.
5. Determine whether this is already a valid Unity project.

If an existing compatible Unity project exists, continue it.

If starting fresh, target a current compatible Unity 6 LTS editor available in the environment.

Use:

- Unity
- C#
- Universal Render Pipeline (URP)
- Unity Input System
- UI Toolkit where appropriate
- Shader Graph where useful

Do not use paid assets.

Do not require external API keys.

Do not silently download questionable third-party packages.

Prefer:

- Unity-supported packages,
- procedural generation,
- Shader Graph,
- particle systems,
- custom primitive/procedural prototype models,
- freely usable built-in resources.

If Unity Editor is unavailable, still create a structurally valid project and clearly report what could not actually be compiled or tested.

Never claim a successful Unity build unless one was actually executed successfully.


==================================================
2. PRODUCT VISION
==================================================

The game starts small.

The player owns:

- one home planet,
- one small spaceship,
- limited resources,
- knowledge of only the immediate surroundings.

Most of the galaxy is unknown.

The player begins in orbit around the home world.

A distant star, planet, or unknown signal becomes visible.

The player physically flies toward it.

As the player approaches, celestial bodies gradually become detectable.

The player scans a planet.

Only then does the game reveal information such as:

- planet type,
- atmosphere,
- temperature,
- mineral richness,
- energy potential,
- habitability,
- life,
- intelligent civilization,
- ownership,
- military strength,
- special discoveries.

The player then decides whether to:

- leave,
- colonize,
- develop,
- exploit resources,
- investigate,
- attack,
- conquer,
- or return later.

The emotional gameplay loop is:

SEE SOMETHING
→ TRAVEL
→ DISCOVER
→ UNDERSTAND
→ DECIDE
→ CHANGE THE WORLD
→ MOVE DEEPER INTO SPACE

The strategic loop is:

EXPLORE
→ COLONIZE
→ PRODUCE
→ BUILD
→ DEFEND
→ CONQUER
→ EXPAND


==================================================
3. VISUAL QUALITY IS A PRIMARY REQUIREMENT
==================================================

This must NOT look like a typical programmer-art Unity prototype.

Gameplay functionality remains the highest priority, but visual quality is the next major priority.

The prototype should look good enough that screenshots and short gameplay clips immediately communicate the fantasy.

Target:

MODERN STYLIZED-REALISTIC SCIENCE FICTION

Do NOT chase photorealism.

Prioritize:

- attractive planets,
- dramatic scale,
- strong lighting,
- atmospheric effects,
- readable silhouettes,
- good materials,
- cinematic composition,
- particle effects,
- motion,
- coherent color palettes,
- smooth transitions,
- satisfying feedback.

Use URP features appropriately.

Where supported and performant, use:

- HDR rendering,
- Bloom,
- Tonemapping,
- Color Adjustments,
- Vignette,
- Anti-aliasing,
- ambient lighting,
- emissive materials,
- Shader Graph,
- particle systems,
- trails,
- reflection probes where useful.

Avoid excessive bloom.

Effects must improve the scene rather than destroy readability.

Create a properly configured global Volume.


==================================================
4. SPACE MUST LOOK GOOD
==================================================

Space must NOT simply be a black background.

Create a layered space environment containing combinations of:

- procedural starfields,
- distant stars,
- subtle nebulae,
- cosmic dust,
- asteroid fields,
- distant celestial silhouettes,
- occasional anomalies.

Do not fill every region with effects.

Some areas should feel empty, dark, and lonely.

Others should feel visually rich.

Each star system should have a slightly different identity.

For example:

HOME SYSTEM:
warm yellow star,
subtle blue nebula,
relatively peaceful.

FRONTIER SYSTEM:
bright blue-white star,
asteroid fields,
purple nebula.

HOSTILE SYSTEM:
red/orange star,
darker environment,
military structures,
denser orbital traffic.


==================================================
5. PLANETS ARE THE VISUAL CENTERPIECE
==================================================

Planets must be one of the strongest visual elements in the prototype.

Create reusable procedural or parameterized planet rendering.

Minimum archetypes:

- Earth-like,
- desert,
- frozen,
- volcanic,
- barren rocky,
- toxic.

Planet rendering should support combinations of:

- base terrain colors,
- secondary terrain variation,
- oceans,
- clouds,
- atmospheric rim,
- night-side shading,
- emissive city lights,
- rotation,
- optional rings,
- optional moons.

Do not require geographically accurate terrain.

Shader-based procedural approximation is acceptable.

A frozen planet must immediately look different from a volcanic planet.

Do not manually implement twelve unrelated planets.

Build reusable planet visual systems and configurations.


==================================================
6. CIVILIZATION MUST BE VISIBLE
==================================================

This is extremely important.

Developing a planet must visibly transform it.

A newly colonized world should look mostly untouched.

As development increases, add visible:

- settlements,
- cities,
- city lights,
- industrial complexes,
- energy installations,
- orbital satellites,
- orbital stations,
- spacecraft traffic.

The player should be able to:

visit a planet,
build a colony,
leave,
return later,
and immediately SEE that civilization has grown.

This visual transformation is part of the reward loop.

Do not simulate individual citizens.

Represent civilization at planetary scale.


==================================================
7. SCALE AND APPROACHING PLANETS
==================================================

Approaching planets must feel impressive.

Use compressed gameplay distances rather than realistic astronomy.

A planet should progress visually from:

distant light/object
→ recognizable world
→ large celestial body
→ massive planet dominating the player's view
→ orbital interaction range.

Avoid obvious popping.

Use LOD or simplified distant representations if necessary.

Planets should feel enormous compared with the player's ship.

The player does NOT land on planets in this prototype.

Planet interaction happens from orbit.


==================================================
8. TRUE 3D SPACE FLIGHT
==================================================

Implement true 3D spaceship movement.

Do NOT use the old top-down/XZ-plane concept.

Use an accessible arcade flight model.

The player should feel like they are piloting a spacecraft, but controls must remain easy to learn.

Default camera:

third-person chase camera behind and slightly above the ship.

Support:

- pitch,
- yaw,
- roll,
- acceleration,
- braking,
- boost,
- weapons,
- target selection,
- scanning.

Mouse + keyboard required.

Gamepad support is desirable if straightforward.

Ship movement should include:

- inertia-inspired movement,
- controlled damping,
- maximum combat speed,
- boost,
- responsive steering,
- visual banking.

Do NOT create realistic orbital mechanics.

This is not Kerbal Space Program.

This is action/adventure space flight.

Camera should support:

- smooth follow,
- subtle lag,
- FOV increase during acceleration,
- restrained camera shake,
- boost feedback,
- optional free-look.

Do not make camera movement nauseating.


==================================================
9. NORMAL FLIGHT AND PULSE DRIVE
==================================================

There are two major travel modes.

NORMAL FLIGHT

Used near:

- planets,
- stations,
- asteroid fields,
- enemies.

Combat works normally.

PULSE / CRUISE DRIVE

Used for interplanetary travel.

When activated:

- ship accelerates dramatically,
- FOV increases,
- engine effects intensify,
- star/dust streaks emphasize speed,
- sound changes if audio is implemented,
- combat becomes disabled or heavily restricted.

Cruise drive disengages when:

- approaching a major planet,
- approaching obstacles,
- entering combat,
- manually cancelled.

Travel must still physically happen.

Do NOT replace travel with teleportation.

Typical travel between nearby prototype planets should take seconds rather than minutes.

Travel to another system should feel meaningful without becoming tedious.


==================================================
10. PROTOTYPE GALAXY
==================================================

Create a small sector containing approximately:

- 3 star systems,
- 12 meaningful planets,
- several moons,
- asteroid fields,
- at least one anomaly.

Suggested layout:

SYSTEM A — HOME SYSTEM

- developed player home world,
- easy empty colonizable world,
- difficult/environmentally hostile planet.

SYSTEM B — FRONTIER SYSTEM

- several unknown planets,
- one neutral civilization,
- resource-rich world,
- asteroid belt,
- anomaly.

SYSTEM C — HOSTILE SYSTEM

- enemy civilization,
- multiple developed enemy planets,
- strong defenses,
- high-value strategic planet.

Use deterministic seeded generation where practical.

Every important celestial body must have a stable identifier.


==================================================
11. DISCOVERY AND FOG OF WAR
==================================================

The player must NOT know everything at the start.

This is critical to the exploration fantasy.

At game start:

- the home system is partially known,
- distant stars can be seen,
- detailed planetary information is hidden.

Use discovery states:

UNKNOWN
DETECTED
SCANNED
VISITED

UNKNOWN:
No meaningful information.

DETECTED:
Approximate position and basic object classification.

SCANNED:
Planet properties become visible.

VISITED:
Full strategic information becomes available.

Undiscovered objects may appear as:

UNKNOWN PLANET
UNKNOWN SIGNAL
UNIDENTIFIED STAR

Discovery state must persist in save data.


==================================================
12. PLANET SCANNING
==================================================

Scanning should be a satisfying gameplay interaction.

The player approaches a planet.

When within scanning range, scanning becomes available.

Scanning:

- takes several seconds,
- requires staying within range,
- has visible progress,
- uses visual effects,
- reveals information progressively.

Possible effects:

- holographic sweep,
- circular scanning wave,
- Shader Graph pulse,
- particles,
- HUD analysis animation.

After completion display a discovery card.

Example:

NEREID IV

FROZEN WORLD

Habitability: LOW
Minerals: VERY HIGH
Energy: MEDIUM
Life: MICROBIAL
Civilization: NONE

Special Trait:
RARE MINERALS

Do not reveal these values before scanning.


==================================================
13. PLANET DATA
==================================================

Planets should have data-driven properties such as:

PlanetType
Temperature
Atmosphere
Habitability
MineralRichness
EnergyPotential
LifeLevel
CivilizationLevel
Owner
DefenseStrength
DevelopmentLevel
SpecialTraits

Example traits:

- Rare Minerals
- Ancient Ruins
- Fertile Biosphere
- Strong Magnetic Field
- Unstable Crust
- Extreme Radiation
- Ancient Technology

Keep the system extensible and data-driven.


==================================================
14. HOME PLANET
==================================================

The home planet should immediately demonstrate what a developed civilization can eventually look like.

It contains visible:

- cities,
- city lights,
- orbital station,
- civilian traffic,
- patrol ships,
- defensive structures.

It produces baseline resources.

It cannot permanently be conquered in this prototype.

Destroyed player ships respawn nearby.


==================================================
15. ECONOMY
==================================================

Use two main resources:

MINERALS
ENERGY

Do not introduce a huge economy.

Research may be added later only if the core prototype is already complete.

Resources are shared across the player's empire.

Planet properties affect production.

Examples:

mineral-rich planet
→ mining bonus

high energy potential
→ energy bonus

high habitability
→ settlement/development bonus


==================================================
16. PLANET DEVELOPMENT
==================================================

Colonized planets support construction.

Implement:

- Mining Complex
- Energy Complex
- Settlement
- Orbital Defense Satellite
- Orbital Station

Use build slots.

Buildings have:

- cost,
- construction time,
- level,
- effect.

Maximum prototype level:

3

Development must affect BOTH:

GAMEPLAY STATISTICS

and

PLANET VISUALS.

Examples:

Mining Complex:
increases mineral production
and creates visible industrial areas.

Settlement:
improves development
and increases visible cities/lights.

Defense Satellite:
creates an actual orbiting weapon.

Orbital Station:
creates a large visible orbital structure.


==================================================
17. ORBITAL ACTIVITY
==================================================

Developed worlds should feel alive.

Create lightweight ambient traffic:

- cargo ships,
- civilian shuttles,
- patrol craft.

They may use simple orbital or spline movement.

Do not fully simulate their economy.

Activity density should increase with planet development.

A home world should feel alive.

A new colony should initially feel lonely.


==================================================
18. COMBAT
==================================================

Implement accessible space combat.

Player ship has:

- shields,
- hull,
- primary energy weapon.

Optional missiles may be added only after basic combat feels good.

Enemies include:

- fighters,
- defense satellites,
- orbital military structures.

Combat should include:

- targeting,
- hit effects,
- shield impacts,
- explosions,
- health feedback,
- target indicators,
- readable enemy silhouettes.

Do not implement dozens of weapons.

Polish a small number.


==================================================
19. PLANETARY DEFENSE
==================================================

Owned planets can construct orbital defense satellites.

Satellites:

- orbit planets,
- detect hostile ships,
- rotate toward targets,
- fire automatically,
- can be destroyed.

Higher development may allow more defense.

Each developed planet may also contain orbital command infrastructure.

Planetary defense must physically exist in the game world.


==================================================
20. COLONIZATION
==================================================

Empty viable planets can be colonized.

Requirements:

- planet scanned,
- player within orbital interaction range,
- enough resources,
- no active hostile defense.

Colonization takes a short amount of time.

Make colonization visually satisfying.

Show:

- colony craft,
- deployment animation,
- orbital effect,
- first settlement appearing,
- faction identification.

Do NOT simply change OwnerId silently.


==================================================
21. EXISTING CIVILIZATIONS
==================================================

Support at least:

NEUTRAL CIVILIZATION

and

HOSTILE AI CIVILIZATION.

Neutral civilizations:

- defend their territory,
- may warn the player,
- do not automatically attack.

No complex diplomacy system is required yet.

Hostile civilizations:

- own planets,
- build infrastructure,
- produce resources,
- construct ships,
- attack and expand.


==================================================
22. CONQUEST
==================================================

To conquer an occupied planet:

1. Reach the system.
2. Scan the world.
3. Defeat local hostile spacecraft.
4. Destroy/disable defense satellites.
5. Disable orbital command infrastructure.
6. Enter capture range.
7. Remain there until capture completes.

Do NOT require destroying civilian cities.

Civilian infrastructure survives conquest.

This makes developed planets valuable targets.

After capture:

- ownership changes,
- previous owner loses production,
- surviving civilian infrastructure remains,
- destroyed military infrastructure must be rebuilt,
- planet gradually adopts the new faction identity.


==================================================
23. PLANETARY PROTECTION SHIELD
==================================================

Implement a temporary strategic planetary shield.

For the prototype it uses ENERGY only.

No real money.
No premium currency.

Suggested prototype values:

Protection:
45 seconds.

Cooldown:
180 seconds after expiration.

Cannot activate if the planet was recently attacked.

Cannot stack.

While active:

- planet cannot be captured,
- orbital infrastructure cannot receive damage,
- protected defense satellites do not attack.

Create a visually impressive translucent planetary energy shield.

All values must be configurable.

Design architecture so monetization could theoretically be considered later, but do NOT implement payments.


==================================================
24. HOSTILE AI EMPIRE
==================================================

Create one competing empire.

AI should:

- own planets,
- generate resources,
- develop planets,
- construct defenses,
- create small fleets,
- choose expansion targets,
- physically travel between planets,
- attack player colonies.

Enemy fleets must launch from actual enemy territory.

Do NOT spawn enemies next to their target.

The player should sometimes see enemy fleets traveling through space.

Provide warnings when detected.

Keep fleet sizes manageable.


==================================================
25. EXPLORATION EVENTS
==================================================

Exploration should contain surprises.

Implement several lightweight events.

Examples:

ANCIENT RUINS

Scanning provides resources or another reward.

ABANDONED SHIP

Player investigates a wreck.

SPACE ANOMALY

Creates an unusual visual location and reward.

UNKNOWN DISTRESS SIGNAL

May lead to wreckage or enemies.

Do not create a massive quest framework.

These exist to reinforce:

"What will I find over there?"


==================================================
26. USER INTERFACE
==================================================

Create a modern spaceship-style HUD.

Avoid default Unity-looking controls.

Main HUD:

- shield,
- hull,
- speed,
- boost,
- cruise status,
- Minerals,
- Energy,
- current target,
- objective,
- contextual interaction.

Target HUD:

- target name,
- distance,
- discovery state,
- hostility,
- health where appropriate.

Scanner UI:

- scanning progress,
- animated analysis,
- planet information,
- special traits.

Planet management UI:

- production,
- development,
- buildings,
- defenses,
- planetary shield,
- ownership.

Sector map:

- discovered systems,
- discovered planets,
- player position,
- owned planets,
- known hostile territory,
- detected unknown signals.

Do NOT reveal undiscovered planets.


==================================================
27. TUTORIAL / FIRST SESSION
==================================================

Create a lightweight first-session tutorial.

Do NOT use long modal tutorial windows.

Teach through gameplay.

The player should feel like they are beginning an adventure, not completing software onboarding.

Tutorial progression must react to actual game state.

FIRST SESSION FLOW:


STEP 1 — HOME ORBIT

Start the player near the developed home planet.

Give the player a few seconds to appreciate:

- planet scale,
- cities,
-
```
lights,
-  orbital station, 
-  traffic. 
Objective:
"Leave home orbit and test your ship."
STEP 2 — BASIC FLIGHT
Teach:
-  acceleration, 
-  turning, 
-  roll, 
-  boost. 
Place a navigation beacon nearby.
Objective:
"Reach the navigation beacon."
STEP 3 — FIRST UNKNOWN SIGNAL
After reaching the beacon, detect a distant object.
Display:
UNKNOWN PLANETARY SIGNAL
Do not reveal what it is.
Objective:
"Investigate the unknown signal."
STEP 4 — PULSE DRIVE
The target should be far enough away that normal travel feels inefficient.
Prompt the player to engage pulse drive.
Objective:
"Engage pulse drive and approach the signal."
Show:
-  acceleration, 
-  FOV change, 
-  engine intensity, 
-  high-speed particles. 
STEP 5 — FIRST PLANET APPROACH
As the player approaches, the unknown object gradually becomes a large planet.
Do not immediately cover it with UI.
Allow a short visual reveal.
Objective:
"Enter scanning range."
STEP 6 — FIRST SCAN
Teach scanning.
Objective:
"Scan the unknown world."
The first world should be a good colonization candidate.
Example:
AURELIA II
EARTH-LIKE WORLD
Habitability: HIGH
 Minerals: MEDIUM
 Energy: HIGH
 Life: SIMPLE
 Civilization: NONE
Trait:
 FERTILE BIOSPHERE
STEP 7 — FIRST COLONY
Give enough starting resources to colonize without grinding.
Objective:
"Establish your first colony."
Show the colony deployment visually.
STEP 8 — FIRST PRODUCTION BUILDING
Teach planet management.
Objective:
"Build an Energy Complex on Aurelia II."
When finished:
-  production increases, 
-  visible infrastructure appears. 
STEP 9 — SETTLEMENT
Objective:
"Expand the colony with a Settlement."
After construction:
-  visible structures increase, 
-  lights appear, 
-  orbital activity increases. 
STEP 10 — PLANETARY DEFENSE
Objective:
"Construct an Orbital Defense Satellite."
Spawn a real satellite.
Make it visibly orbit the planet.
STEP 11 — DISTANT STAR DETECTED
Reveal a previously unidentified star.
Objective:
"A distant signal has been detected. Travel beyond the home system."
STEP 12 — TRAVEL TO FRONTIER SYSTEM
Player physically travels there.
Make arrival visually different.
Objective:
"Enter the Frontier System."
STEP 13 — EXPLORATION EVENT
Introduce an unknown anomaly before another normal planet.
Objective:
"Investigate the anomalous signal."
Possible result:
ANCIENT RUINS
or
ABANDONED SHIP
Give a useful reward.
STEP 14 — DISCOVER INTELLIGENT LIFE
Guide player toward another unknown planet.
Scanning reveals:
Civilization:
 INDUSTRIAL
Military Activity:
 DETECTED
Objective:
"Observe the unidentified civilization."
Do not immediately order the player to attack.
STEP 15 — FIRST CONTACT
Civilization warns the player.
Example:
"Unidentified vessel. You have entered restricted space."
Hostile fighters eventually approach.
Objective:
"Defend yourself."
STEP 16 — FIRST COMBAT
Teach:
-  targeting, 
-  firing, 
-  shields, 
-  maneuvering. 
Use only a few weak fighters.
Objective:
"Destroy the hostile interceptors."
STEP 17 — COLONY WARNING
After combat:
HOSTILE ACTIVITY DETECTED NEAR AURELIA II
Objective:
"Return to Aurelia II."
STEP 18 — DEFEND YOUR WORLD
Enemy ships physically travel toward the colony.
Player returns.
The defense satellite previously constructed participates.
Objective:
"Defend Aurelia II."
This is a major emotional moment:
the player is personally defending something they built.
STEP 19 — PLANETARY SHIELD
After the battle, introduce planetary protection.
Explain briefly:
-  Energy cost, 
-  duration, 
-  cooldown. 
Objective:
"Review Aurelia II's planetary protection options."
Activation may remain optional.
STEP 20 — DISCOVER THE ENEMY EMPIRE
Information recovered from attackers reveals hostile territory.
Update sector map.
Objective:
"Locate the source of the attack."
STEP 21 — PREPARE AN OFFENSIVE
Ensure:
-  player ship is repaired, 
-  resources are available, 
-  colonies are productive. 
Avoid grinding.
Objective:
"Prepare your ship for an offensive expedition."
STEP 22 — ENTER HOSTILE TERRITORY
Player physically travels into the hostile system.
Make arrival visually memorable.
Show:
-  developed enemy planet, 
-  patrols, 
-  defense satellites, 
-  stations. 
Objective:
"Enter hostile territory."
STEP 23 — SCAN ENEMY PLANET
Reveal:
-  production value, 
-  development, 
-  military strength, 
-  defenses. 
Objective:
"Scan the enemy colony and identify its defenses."
STEP 24 — PLANETARY ASSAULT
Player attacks:
-  enemy fighters, 
-  defense satellites, 
-  orbital command infrastructure. 
Objective:
"Neutralize the orbital defenses."
STEP 25 — FIRST CONQUEST
After defenses are disabled:
Objective:
"Take control of the enemy colony."
Player enters capture range.
Show:
-  capture progress, 
-  faction transition, 
-  surviving civilization, 
-  military shutdown. 
Do not destroy civilian infrastructure.
STEP 26 — SHOW CONSEQUENCES
After conquest:
-  player income increases, 
-  enemy income decreases, 
-  sector ownership changes, 
-  captured planet becomes manageable. 
Communicate this visually rather than with a huge tutorial window.
STEP 27 — END GUIDED TUTORIAL
Display:
THE FRONTIER IS OPEN
Explore unknown systems.
 Build your civilization.
 Defend your worlds.
 Conquer the galaxy.
Remove mandatory tutorial objectives.
The game becomes a sandbox.
Tutorial progression must be state-driven.
Examples:
"Build Settlement"
completes only after construction actually finishes.
"Scan Planet"
completes only after discovery state becomes SCANNED.
"Capture Planet"
completes only after ownership actually changes.
Tutorial state must survive save/load.
If the player already performs a required action early, detect that state rather than forcing repetition.
Prefer:
-  contextual HUD hints, 
-  navigation markers, 
-  transmissions, 
-  objective tracker, 
-  subtle highlights, 
-  world events. 
Avoid:
-  huge modal windows, 
-  walls of text, 
-  constant interruption, 
-  fake cursors, 
-  excessive hand-holding. 
Target approximately:
20–35 minutes
for a complete first guided session.
Do not artificially extend the tutorial using:
-  grinding, 
-  long waiting, 
-  excessive travel, 
-  huge construction timers, 
-  repeated battles. 
During tutorial mode use accelerated:
-  construction, 
-  resource generation, 
-  travel, 
-  AI timing. 
Keep these values configurable.
The tutorial must be difficult to softlock.
If the player dies:
-  respawn near home planet, 
-  preserve empire progress, 
-  resume objective. 
If resources are accidentally spent:
-  passive production must recover reasonably quickly, 
-  or provide limited tutorial recovery. 
If an essential enemy disappears unexpectedly:
-  detect the state, 
-  recover or advance appropriately. 
If player travels away:
-  do not reset progress, 
-  provide navigation guidance. 
If save is loaded:
reconstruct current objective from authoritative state.
After the tutorial the game becomes a sandbox.
Player can:
-  explore remaining systems, 
-  discover planets, 
-  scan worlds, 
-  investigate anomalies, 
-  colonize, 
-  develop civilizations, 
-  build orbital infrastructure, 
-  construct defenses, 
-  protect worlds, 
-  fight enemy fleets, 
-  capture hostile planets. 
AI continues operating independently.
Do not stop simulation when tutorial ends.
Prototype victory:
Control at least:
8 OF 12 PLANETS
for:
60 CONTINUOUS SECONDS.
If ownership drops below 8:
reset countdown.
Show progress unobtrusively.
Victory presentation may include:
-  camera pullback, 
-  sector overview, 
-  owned planets, 
-  empire statistics. 
After victory allow:
CONTINUE EXPLORING.
Use versioned JSON save files in:
Application.persistentDataPath
Persist:
-  galaxy seed, 
-  celestial IDs, 
-  discovery states, 
-  planet ownership, 
-  development, 
-  buildings, 
-  resources, 
-  player ship state, 
-  player position, 
-  AI state, 
-  fleets, 
-  shields, 
-  construction, 
-  tutorial state, 
-  anomalies, 
-  victory progress. 
Do NOT serialize GameObject references.
Use stable IDs and DTOs.
Loading reconstructs presentation from authoritative saved state.
Prevent:
-  duplicate entities, 
-  duplicate event subscriptions. 
Use safe save replacement:
temporary file
 → validation
 → main save.
Keep backup if practical.
Corrupt saves must not silently destroy the previous valid save.
Separate game rules from presentation.
Suggested structure:
Core/
-  Economy 
-  Ownership 
-  Construction 
-  Discovery 
-  Colonization 
-  Conquest 
-  Protection 
-  Victory 
Simulation/
-  Galaxy 
-  Planets 
-  Factions 
-  Fleets 
-  AI 
Flight/
-  PlayerController 
-  Weapons 
-  Targeting 
-  Scanner 
-  CruiseDrive 
Presentation/
-  PlanetVisuals 
-  CivilizationVisuals 
-  VFX 
-  Camera 
-  OrbitalTraffic 
UI/
-  HUD 
-  ScannerUI 
-  PlanetManagement 
-  SectorMap 
-  Tutorial 
Persistence/
-  DTOs 
-  SaveService 
-  Migrations 
Avoid one giant GameManager.
Also avoid overengineering.
Do NOT use:
-  microservices, 
-  networking yet, 
-  ECS/DOTS unless absolutely justified, 
-  external DI framework, 
-  generic repositories everywhere. 
Use explicit dependencies and interfaces where useful.
Keep authoritative game state separate enough from Unity presentation that future server-authoritative multiplayer remains feasible.
Use ScriptableObjects or equivalent configuration for:
-  planet archetypes, 
-  buildings, 
-  ships, 
-  weapons, 
-  economy, 
-  scanning, 
-  pulse drive, 
-  AI, 
-  protection, 
-  tutorial pacing, 
-  VFX settings. 
Avoid magic numbers scattered throughout MonoBehaviours.
Separate runtime state from definitions.
Target smooth desktop gameplay.
Use sensible optimization:
-  object pooling for frequently spawned objects, 
-  LOD, 
-  simplified distant traffic, 
-  reduced AI simulation frequency when far away, 
-  avoid per-frame allocations, 
-  avoid FindObjectsOfType-style searches every frame. 
Do not prematurely optimize at the expense of finishing the prototype.
Create an idempotent editor setup command:
Tools
 → Orbit Dominion
 → Setup Prototype
It should create/configure:
-  main scene, 
-  URP, 
-  post-processing, 
-  Input Actions, 
-  default configuration assets, 
-  UI resources, 
-  prototype materials, 
-  bootstrap systems. 
Avoid requiring manual Inspector wiring.
If procedural runtime generation is used, setup may create the bootstrap scene and required configuration.
Do not invent invalid Unity YAML or fake GUIDs.
If Unity Editor exists, execute setup and verify it.
Add EditMode tests for:
-  discovery transitions, 
-  scanning, 
-  colonization requirements, 
-  resource costs, 
-  production ownership, 
-  construction, 
-  conquest requirements, 
-  planetary shields, 
-  AI ownership changes, 
-  tutorial objective completion, 
-  save/load, 
-  victory countdown. 
Add PlayMode smoke tests for:
-  main scene loading, 
-  exactly one player spawning, 
-  galaxy generation, 
-  planet detection, 
-  scanning, 
-  colonization, 
-  restarting without duplicated systems. 
Where practical test:
-  civilization visuals respond to development, 
-  defense satellite appears after construction, 
-  save/load reconstructs discovered planets. 
Once the full gameplay loop works:
STOP ADDING MAJOR FEATURES.
Perform a dedicated visual polish pass.
Review and improve:
-  planet shaders, 
-  atmospheric rims, 
-  clouds, 
-  oceans, 
-  night-side city lights, 
-  star lighting, 
-  nebulae, 
-  home planet presentation, 
-  ship engine VFX, 
-  pulse drive VFX, 
-  scanner VFX, 
-  weapon impacts, 
-  shields, 
-  explosions, 
-  planetary protection shield, 
-  orbital stations, 
-  orbital traffic, 
-  UI animation, 
-  camera movement, 
-  sense of scale. 
The goal is to make existing systems feel premium rather than add more systems.
If screenshots can be generated, inspect representative screenshots and fix obvious visual weaknesses.
The prototype succeeds when the player can:
1.  Start near a visually developed home planet. 
2.  Personally fly a 3D spacecraft. 
3.  Detect an unknown signal. 
4.  Travel using pulse drive. 
5.  Approach a convincing large planet. 
6.  Scan it. 
7.  Reveal previously unknown information. 
8.  Colonize it. 
9.  Build infrastructure. 
10.  See civilization visibly appear. 
11.  Build an orbital defense satellite. 
12.  Travel to another star system. 
13.  Discover an anomaly. 
14.  Discover intelligent life. 
15.  Fight enemy spacecraft. 
16.  Return to defend their own colony. 
17.  Fight alongside their own orbital defenses. 
18.  Discover hostile territory. 
19.  Travel there personally. 
20.  Scan an enemy world. 
21.  Destroy orbital defenses. 
22.  Capture the planet. 
23.  See ownership change. 
24.  Gain the captured planet's economy. 
25.  Save the game. 
26.  Load it without losing meaningful state. 
27.  Continue exploring freely. 
The player should finish the tutorial thinking:
"I started with one ship and one planet, discovered unknown worlds, created a colony, watched it grow, defended it, and then traveled across space to conquer another civilization."
Do NOT attempt everything simultaneously.
Build vertical slices.
PHASE 1 — FLIGHT & VISUAL FOUNDATION
-  Unity project 
-  URP 
-  space environment 
-  home planet 
-  player ship 
-  chase camera 
-  flight 
-  pulse drive 
Make this visually attractive before proceeding too far.
PHASE 2 — DISCOVERY
-  unknown planet 
-  detection 
-  approach 
-  scanning 
-  planet information 
-  discovery UI 
PHASE 3 — COLONIZATION
-  resources 
-  colonization 
-  first settlement 
-  planet management 
-  visible civilization growth 
PHASE 4 — DEVELOPMENT
-  mining 
-  energy 
-  settlement upgrades 
-  orbital station 
-  defense satellite 
-  orbital traffic 
PHASE 5 — COMBAT
-  weapons 
-  shields 
-  enemy fighters 
-  targeting 
-  defense satellites 
-  VFX 
PHASE 6 — GALAXY
-  three systems 
-  twelve planets 
-  pulse travel 
-  frontier 
-  anomalies 
-  neutral civilization 
PHASE 7 — WAR
-  hostile empire 
-  AI economy 
-  fleets 
-  colony attacks 
-  conquest 
PHASE 8 — GAME SYSTEMS
-  sector map 
-  planetary shield 
-  save/load 
-  victory 
-  tutorial state 
PHASE 9 — POLISH
-  visual polish 
-  VFX 
-  UI 
-  camera 
-  balancing 
-  tests 
-  performance 
IMPORTANT:
A beautiful, fun, polished first star system is more valuable than twelve broken planets.
Prioritize the player's emotional experience of:
FLYING
 → SEEING SOMETHING UNKNOWN
 → APPROACHING IT
 → DISCOVERING IT
before implementing deep strategy.
Maintain:
IMPLEMENTATION_PLAN.md
Include:
-  current phase, 
-  completed features, 
-  known issues, 
-  next tasks, 
-  systems intentionally deferred. 
Update AGENTS.md with useful project-specific instructions without deleting existing instructions.
README.md must explain:
-  required Unity version, 
-  how to open project, 
-  how to run setup, 
-  controls, 
-  game loop, 
-  project architecture, 
-  configuration, 
-  save location, 
-  tests, 
-  build instructions, 
-  known limitations. 
Do not finish by merely saying code was created.
Actually verify as much as the environment allows.
If Unity Editor is available:
1.  Run project setup. 
2.  Import/compile scripts. 
3.  Fix compilation errors. 
4.  Run EditMode tests. 
5.  Run PlayMode tests. 
6.  Open/play the main scene where possible. 
7.  Create a Windows build if the required build module exists. 
8.  Review logs for errors. 
In the final report clearly separate:
IMPLEMENTED
VERIFIED / EXECUTED
NOT VERIFIED
KNOWN LIMITATIONS
HOW TO RUN
Do not claim tests passed if they were not executed.
Do not claim a Windows build exists if one was not actually generated.
Do NOT implement:
-  real multiplayer, 
-  accounts, 
-  backend servers, 
-  matchmaking, 
-  microtransactions, 
-  payment processing, 
-  walking on planets, 
-  FPS gameplay, 
-  detailed planetary terrain, 
-  individual citizen simulation, 
-  hundreds of star systems, 
-  huge procedural galaxies, 
-  complex diplomacy, 
-  dozens of resources, 
-  dozens of ship classes. 
Those belong to later development.
The purpose of this prototype is to prove that this combination is fun:
PERSONALLY FLY THROUGH SPACE
 +
 DISCOVER UNKNOWN WORLDS
 +
 BUILD VISIBLE CIVILIZATIONS
 +
 DEFEND YOUR PLANETS
 +
 CONQUER OTHER WORLDS.
Throughout development continually ask:
"Does this make me want to fly toward that planet?"
"Does approaching the planet feel impressive?"
"Do I feel that I discovered something rather than selected something from a menu?"
"Can I visually see what I built?"
"Do I care when my colony is attacked?"
"Does capturing another developed world feel meaningful?"
If the answer is no, improve those experiences before adding more mechanics.
The strongest identity of Orbit Dominion should be:
You are not an invisible emperor looking at a map.
You are physically inside the empire you are building.
You personally discover its worlds.
You personally travel between them.
You can see civilization spreading across their surfaces.
And when another empire comes for one of those worlds, you can fly there yourself and fight for it.
START NOW.
Inspect the environment first.
Then implement PHASE 1.
Do not stop after planning.
Create the actual playable Unity project.