# Orbit Dominion — implementation plan

## Current milestone
The original prototype phases (1–9) are implemented. Later user requests expand the game with landing, surface exploration and a creative planet sandbox. The integrated loop now includes physical flight, discovery, planetary expeditions, rover traversal, road networks, modular settlements, repeatable planet activities, colonization, visible development, combat, inter-system exploration, defense, conquest, full saving and continued sandbox play. The current photoreal-direction pass layers generated PBR texture detail, cutout foliage, local environment reflections, higher-resolution rendering and shadows, and procedural stellar surfaces over the existing integrated game. Verification evidence is in Documentation/VERIFICATION.md and Documentation/Reports.

## Implemented phases
1. Unity 6 / URP foundation: procedural space, developed home planet, physical flight/boost/pulse, camera, input, setup command and Windows build.
2. Discovery: unknown/detected/scanned/visited states, held scans, hidden properties, scanner effects and journal.
3. Colonies: shared minerals/energy, viable-world checks, timed deployment craft, settlement, production and ownership.
4. Development: mining, energy, settlements, satellites, stations, three levels, build slots, queued construction, cities and traffic.
5. Combat: energy weapons, shields/hull, target selection, occlusion, factions, interceptors, orbital defenses, effects and rescue.
6. Galaxy: Solace, Veil Frontier and Cinder Reach; twelve stable-ID planets; Earth/desert/frozen/volcanic/rocky/toxic archetypes; moons, physical travel, three investigation sites, rewards and a neutral civilization.
7. War: Cinder Dominion ownership/economy, paid infrastructure/ship production, bounded fleets launched from enemy planets, physical fleet transit, colony warnings/attacks/occupation and player conquest. Civilian buildings survive ownership transfers; military capacity is cleared.
8. Game systems: sector chart/navigation, 45-second energy-funded protection with 180-second cooldown after expiration and recent-attack lock, 27 state-driven guided stages, eight-of-twelve ownership held for sixty continuous seconds, continued sandbox after victory, and atomic versioned full-flight saves with backup recovery and legacy migration.
9. Polish: system lighting/nebula identities, archetype-specific ice/cloud/rim/lava shading, shield and anomaly visuals, combat-focused HUD, button transitions, pooled combat effects, distant planet meshes, reduced distant AI frequency, procedural mesh cleanup, configurable balance, regression/integration tests, real Windows captures and frame samples.

## Expansion — surface exploration

The user's later request supersedes the original brief's orbit-only restriction. The next vertical slice adds controlled landing from the scanned planet's orbit, a distinct deterministic local surface for every existing world, third-person traversal, biomes including Elysia's open ocean, flora/fauna or citizens where appropriate, six hidden recoverable relics per world, surface scanning, return to the parked ship, and persistence of the active expedition and collected relics. The existing space/war game remains playable.

The local landing region is deliberately finite. Future expansion may add several regions per planet, seamless descent/global terrain, NPC conversation and quests, ground combat and deeper crafting. The user explicitly deferred the larger graphics/space-realism/combat overhaul; those are not part of this slice.

## Surface visual pass

The later surface request adds a shared natural landscape model to the landing regions: a dry arrival area, deterministic coastline and submerged outer edge, an inlet visible from the temperate landing view, and a raised district for inhabited worlds. Biome palettes connect soil, slope rock, beaches, water and atmospheric colors. Procedural terrain, animated water and clouded sky use dedicated URP shaders retained by idempotent setup for standalone builds. The realism pass adds multiscale terrain breakup, wet shore response, water normals/reflection/foam, layered cloud lighting, soft directional shadows, ACES color finishing and enhanced orbital planet oceans, land detail, cloud shadows and atmospheric rims. The latest pass adds generated seamless terrain, construction-panel and road albedo/height-derived normal maps; alpha-cutout temperate foliage; textured rover, ship, colony and surface-building materials; one-shot local reflection probes; 1.1 render scale; a 4096 main-light shadow atlas; and animated granular photosphere/corona shaders for all three stars. Inhabited districts retain radial roads and street lighting. Elysia remains an open-water world, while arid, frozen, volcanic, rocky and toxic worlds vary the same land/water logic. This is a photoreal direction for the existing finite procedural prototype, not a claim of a production AAA scanned-asset pipeline.

## Detailed surface models — 2026-11-19

The surface model pass replaces primitive building silhouettes with metre-scaled pressure modules: bevelled wall panels, inset glazing, sunshades, airlocks, roof ventilation and collectors, industrial tanks/pipes, and a marked landing apron. All nine building types retain their saved enum values, footprints, costs and collision rules. Inhabited districts reuse the same architectural kit. The rover now has shaped bodywork, framed windows, a rocker/bogie suspension, separate tyre/rim/tread geometry, fasteners, cameras and a jointed sampling tool. Wheels keep independent rotating roots; flotation remains available.

Natural scenery uses tapered trunks/roots, denser leaf clusters, ribbed fungal caps, irregular eroded outcrops, fractured ice and bent grass blades. Trees, fungal groves and large outcrops are hidden where saved lots overlap them, reconstructed consistently on load, and restored when their last overlapping lot is recycled. Land fauna spawns only where its small wandering path stays above the waterline. Static geometry combines by material within each model, with explicit mesh ownership and disposal. Daylight direction matches the local sky, and a custom diffuse ambient probe keeps shadowed models readable while preserving the original orbital lighting on return. Integration checks cover building bounds, valid mesh faces/tangents, disposal, lighting restoration and scenery clearance through save/load/recycling. The sandbox capture includes close rover and settlement inspection views. Actual Unity verification status is recorded separately in Documentation/VERIFICATION.md.

## Surface rover

The user requested driving a Mars-rover-like vehicle on planetary surfaces. On landing, a six-wheel rover with cabin, solar wings, sensors and relic manipulator replaces the walking explorer. W/S accelerate, brake and reverse; A/D steer, Shift increases forward speed, and the camera follows the rover with temporary mouse look. The vehicle follows terrain gradients, spins its wheels and deploys flotation pontoons on water, retaining access to Elysia and submerged relic sites. The HUD shows rover speed and mode. SurfaceRoverRules owns motion; the expedition adapts keyboard input, terrain, camera and presentation. Existing optional SurfaceSave position/heading and relic IDs remain compatible; speed is transient and resumes at rest.

## Expansion — creative planets and activities

The current direction uses Minecraft as the reference for readable grid construction and Roblox as the reference for varied repeatable activities. It does not copy their art, assets, UI or protected expression. Owned planets expose a builder with nine modular types: driveable foundations, walls, ramps, pressurized habitats, transmission beacons, road tiles, inhabited arcologies, industrial complexes and spaceports. Modules snap to a four-metre horizontal grid, support six vertical levels and four rotations, spend empire minerals/energy and return half their cost when recycled. Authoritative footprints reject overlapping lots while allowing connected neighboring roads and vertically separated construction. Foundations, ramps, roads and spaceports alter rover floor height; occupied structures block vehicle movement. Presentation combines block-scale construction with material-specific roughness, metal framing, curbs, road markings, windows, pipes, tanks, collectors, service areas and emissive navigation details.

Every planet now has a deterministic seven-gate Rover Rally. The player reaches the highlighted start gate, presses T, follows gates in order and records a per-world best time. First completion and new records feed rewards into the empire economy. `SurfaceSandboxRules` and `SurfaceSandboxState` own placement, collision, costs, rally routes and records; `SurfaceExpedition` adapts input and presentation. The optional `SurfaceSave.sandbox` payload retains stable build IDs, positions, rotations and rally records. Old flight-v2.json saves omit it and remain valid.

This is the first activity framework slice. Curated modes, blueprint sharing and cooperative play can build on the same planet/activity boundary. Player-authored scripts, public publishing, moderation, accounts and servers remain later platform work rather than hidden requirements of the local prototype.

## Stellar systems and orbital motion

The three visible stars now occupy the centers of their own planetary systems. Each planet follows a deterministic, mildly eccentric XZ-plane Kepler path around its system's star: orbital period grows with the 3/2 power of semimajor axis and varies with the system's stellar mass. The authored positions are the zero-time epoch, retaining the original opening scene and older save coordinates. Moons revolve around their parent planets on shorter inclined visual paths, while faint orbit lines make the system layout legible at a distance. Starlight points away from the star in the player's current system. Ships cannot enter stellar surfaces or engage pulse within their exclusion zone.

The orbital clock pauses with simulation and is an optional additive field in flight-v2.json; an older archive defaults to epoch zero. Planetary structures, protection domes, nearby garrisons and outposts follow their world. Fleets update destinations as planets travel. A parked low-speed ship co-moves with its nearby planet, including while the rover is deployed, so launch returns to the same orbit. GalaxyCatalog.Centers remain stable navigation waypoints and system boundaries; stars are separate physical centers. These are accelerated game-scale orbits, not a literal scale model of Solar System distances or periods.

## Important behavior
- M selects navigation signals; it never teleports the ship. Unknown planet names and ownership remain hidden until the relevant discovery stage.
- Neutral defenses warn and retaliate after aggression. Hostile fleets originate in actual enemy-owned territory; first guided attacks wait until the early colony sequence is ready.
- Capture requires scanning, local fighters/defenses/command cleared, pulse off, holding G continuously within orbital range, and no active protection. Leaving range/releasing G resets progress.
- Planetary protection prevents capture and infrastructure damage and suspends protected weapons. It costs only energy and cannot stack or activate immediately after attack.
- Home ownership cannot be lost. Player rescue preserves economy, planets and tutorial progression. Meridian repairs a living ship after three quiet seconds in orbit.
- New flight-v2.json saves ship pose/velocity/pulse/vitals, discoveries, all owners/buildings/resources/construction, combat actors, fleet orders, AI resources/timer, protection/cooldowns/capture, anomalies, tutorial flags and victory progress.
- Old three-world discovery-v1.json / empire-v1.json migrate into the expanded sector without rewriting the originals. Invalid current saves preserve the primary and can recover a validated backup; saving over an invalid primary is blocked.
- The tutorial reads completed authoritative actions rather than just key presses, recognizes early work, retains progress through death/reload, and ends in an active sandbox. Victory does not stop simulation.

## Verification and delivery
- Use Tools/Verify-Unity.ps1 -Build for setup, EditMode, PlayMode and Windows build.
- Use -orbitCapture, -systemCapture, -combatCapture and -sectorCapture for isolated visual QA; user saves are excluded. System QA shows each star and its orbits at epoch zero and after twenty accelerated minutes. Sector QA stages positions and preconditions and exercises real investigation/capture/protection/save rules; it is not a human walkthrough.
- Tests cover previous flight/discovery/building/combat behavior plus galaxy counts/archetypes, protection/cost/cooldown, capture continuity, home ownership, victory reset, dynamic survey ownership, surface construction/collision/rally persistence, map/I input, AI economy/fleet travel/occupation, conquest preservation, tutorial construction, full-save scene reconstruction, corruption and old-save migration.
- Current counts, timestamps, player outcomes and hashes belong in Documentation/Reports/build-summary.json, not assumptions about unexecuted checks.

## Known limits and follow-up QA
- The supplied scope is a playable prototype, not a production release. Art uses procedural geometry with generated PBR bitmap detail and dedicated shaders; it does not yet have production-authored models, motion capture or a global scanned-material library. Orbital city sites may overlap ocean/cloud regions; ring debris is visual only.
- A full uninterrupted human-controlled 20–35-minute tutorial/campaign walkthrough, multi-hour endurance run, low-end hardware and other operating systems have not been verified. Automated frame samples are specific to this machine and staged views.
- No audio or complete gamepad bindings. Enemy tactics remain intentionally simple; no complex diplomacy or logistics.
- URP may warn about stripped unused depth-of-field/Panini effects, which are not enabled.
- Multiplayer, accounts/backends, monetization, FPS combat, detailed terrain/citizens and hundreds of systems remain outside the current prototype. Landing is included by the later user request.

Further work should respond to settlement and driving playtests before adding terrain modification, resource harvesting, blueprint libraries, production-grade authored art, more curated activities or any multiplayer/community backend.
