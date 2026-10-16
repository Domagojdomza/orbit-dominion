using UnityEngine;
using UnityEngine.InputSystem;

namespace OrbitDominion
{
    public sealed class PlayerFlight : MonoBehaviour
    {
        public FlightSettings settings;
        public PlanetView[] worlds;
        public Vector3 Velocity { get; private set; }
        public float Speed => Velocity.magnitude;
        public bool Pulse { get; private set; }
        public bool Boosting { get; private set; }
        public bool Paused { get; private set; }
        public string Status { get; private set; } = "FLIGHT ASSIST ONLINE";
        public float Clearance
        {
            get
            {
                float nearest=float.MaxValue;
                foreach(var world in worlds)nearest=Mathf.Min(nearest,Vector3.Distance(transform.position,world.transform.position)-world.Definition.radius);
                foreach(var star in GalaxyCatalog.StarCenters)nearest=Mathf.Min(nearest,Vector3.Distance(transform.position,star)-GalaxyCatalog.StarRadius);
                return nearest;
            }
        }
        public bool HasPulsed { get; private set; }
        public float DistanceTravelled { get; private set; }
        InputActionAsset actions;
        InputAction throttle, steer, roll, boost, brake, pulse, pause;
        InputAction scan, target, journal, save, colonize, manage;
        InputAction fire,combatTarget;
        float combatLock;
        public bool FlightDisabled {get;set;}
        public float CombatLock=>combatLock;
        InputAction map,capture,protect,investigate,land;
        public bool LandPressed=>land!=null&&land.WasPressedThisFrame();
        public bool MapPressed=>map!=null&&map.WasPressedThisFrame();
        public bool CaptureHeld=>capture!=null&&capture.IsPressed();
        public bool ProtectPressed=>protect!=null&&protect.WasPressedThisFrame();
        public bool InvestigateHeld=>investigate!=null&&investigate.IsPressed();
        public void RestoreFlight(GameSave save){transform.SetPositionAndRotation(save.playerPosition,save.playerRotation);Velocity=save.velocity;Pulse=save.pulse;combatLock=save.combatLock;}
        public bool FireHeld=>fire!=null&&fire.IsPressed();
        public bool CombatTargetPressed=>combatTarget!=null&&combatTarget.WasPressedThisFrame();
        public void StopFlight(){Velocity=Vector3.zero;Pulse=false;Boosting=false;}
        public void InhibitPulse(float seconds){combatLock=Mathf.Max(combatLock,seconds);Pulse=false;Status="COMBAT / PULSE INHIBITED";}
        public bool ColonizePressed=>colonize!=null&&colonize.WasPressedThisFrame();
        public bool ManagePressed=>manage!=null&&manage.WasPressedThisFrame();
        public bool ScanHeld=>scan!=null&&scan.IsPressed();
        public bool TargetPressed=>target!=null&&target.WasPressedThisFrame();
        public bool JournalPressed=>journal!=null&&journal.WasPressedThisFrame();
        public bool SavePressed=>save!=null&&save.WasPressedThisFrame();
        public void Initialize(InputActionAsset source)
        {
            actions = Instantiate(source);
            throttle = actions.FindAction("Throttle", true); steer = actions.FindAction("Steer", true);
            roll = actions.FindAction("Roll", true); boost = actions.FindAction("Boost", true);
            brake = actions.FindAction("Brake", true); pulse = actions.FindAction("Pulse", true); pause = actions.FindAction("Pause", true);
            scan=actions.FindAction("Scan",true);target=actions.FindAction("Target",true);journal=actions.FindAction("Journal",true);save=actions.FindAction("SaveSurvey",true);
            colonize=actions.FindAction("Colonize");manage=actions.FindAction("Manage");
            fire=actions.FindAction("Fire");combatTarget=actions.FindAction("CombatTarget");
            map=actions.FindAction("SectorMap");capture=actions.FindAction("Capture");protect=actions.FindAction("Protect");investigate=actions.FindAction("Investigate");
            land=actions.FindAction("Land");
            actions.Enable();
            SetPause(false);
        }
        void OnDestroy() { if(actions != null) { actions.Disable(); Destroy(actions); } Time.timeScale = 1; Cursor.lockState = CursorLockMode.None; Cursor.visible = true; }
        public void SetPause(bool value)
        {
            Paused = value; Time.timeScale = value ? 0 : 1; Cursor.lockState = value ? CursorLockMode.None : CursorLockMode.Locked; Cursor.visible = value;
        }
        public void ReturnHome()
        {
            Vector3 home=worlds!=null&&worlds.Length>0?worlds[0].transform.position:Vector3.zero;
            Vector3 offset=worlds!=null&&worlds.Length>0?PrototypeBootstrap.StartPosition-worlds[0].Definition.center:PrototypeBootstrap.StartPosition;
            transform.SetPositionAndRotation(home+offset, PrototypeBootstrap.StartRotation);
            Velocity = Vector3.zero; Pulse = false; Status = "RETURNED TO HOME ORBIT";
        }
        void Update()
        {
            if (actions == null) return;
            if (pause.WasPressedThisFrame()) SetPause(!Paused);
            if (Paused||FlightDisabled) return;
            if(combatLock>0){combatLock=Mathf.Max(0,combatLock-Time.deltaTime);if(combatLock<=0)Status="FLIGHT ASSIST ONLINE";}
            float dt = Mathf.Min(Time.deltaTime,.05f);
            if (pulse.WasPressedThisFrame())
            {
                Pulse = !Pulse && combatLock<=0 && FlightRules.CanPulse(Clearance, settings.pulseExclusion);
                Status = Pulse ? "PULSE DRIVE ENGAGED" : Clearance <= settings.pulseExclusion ? "PULSE INHIBITED / GRAVITY WELL" : "PULSE DRIVE DISENGAGED";
            }
            if (Pulse && !FlightRules.CanPulse(Clearance, settings.pulseExclusion)) { Pulse = false; Status = "ARRIVAL / PULSE DISENGAGED"; }
            bool obstacleAhead = Speed>1 && Physics.SphereCast(transform.position, settings.shipClearance, Velocity.normalized,
                out _, Speed*Speed/(2*settings.braking)+30, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            if(obstacleAhead) { Pulse=false; Status="OBSTACLE / AUTOMATIC BRAKING"; }
            if (brake.IsPressed()) { Pulse = false; Status = "INERTIAL BRAKES"; }
            Boosting = boost.IsPressed() && !Pulse && !brake.IsPressed();
            Vector2 look = steer.ReadValue<Vector2>();
            // Mouse delta is already per-frame; keyboard/gamepad steering uses angular speed.
            bool mouse = steer.activeControl?.device is Mouse;
            Vector2 turn = mouse ? look * settings.mouseSensitivity : look * new Vector2(settings.yawRate, settings.pitchRate) * dt;
            turn = Vector2.ClampMagnitude(turn, 5);
            transform.Rotate(-turn.y * (Pulse ? .45f : 1), turn.x * (Pulse ? .45f : 1), -roll.ReadValue<float>() * settings.rollRate * dt, Space.Self);
            float input = throttle.ReadValue<float>();
            float targetSpeed = Pulse ? FlightRules.ApproachSpeed(Clearance,settings.pulseExclusion,settings.normalSpeed,settings.pulseSpeed)
                : brake.IsPressed() || obstacleAhead ? 0 : input > 0 ? (Boosting ? settings.boostSpeed : settings.normalSpeed) : input < 0 ? -settings.normalSpeed * .3f : 0;
            Vector3 desired = transform.forward * targetSpeed;
            float acceleration = Pulse ? settings.pulseAcceleration : brake.IsPressed() || obstacleAhead ? settings.braking : settings.acceleration;
            Velocity = Vector3.MoveTowards(Velocity, desired, acceleration * dt);
            Vector3 proposed = transform.position + Velocity * dt;
            if(Speed>.01f && Physics.SphereCast(transform.position,settings.shipClearance,Velocity.normalized,out RaycastHit impact,Speed*dt,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore))
            {
                proposed=transform.position+Velocity.normalized*Mathf.Max(0,impact.distance-.1f);Velocity=Vector3.zero;Pulse=false;
            }
            foreach (var world in worlds)
            {
                Vector3 safe = FlightRules.SafePosition(proposed,world.transform.position,world.Definition.radius+settings.shipClearance,transform.up);
                if (safe != proposed) { proposed = safe; Velocity = Vector3.zero; Pulse = false; Status = "SURFACE PROXIMITY / FLIGHT ASSIST"; }
            }
            foreach(var star in GalaxyCatalog.StarCenters)
            {
                Vector3 safe=FlightRules.SafePosition(proposed,star,GalaxyCatalog.StarRadius+settings.shipClearance,transform.up);
                if(safe!=proposed){proposed=safe;Velocity=Vector3.zero;Pulse=false;Status="STELLAR PROXIMITY / FLIGHT ASSIST";}
            }
            DistanceTravelled += Vector3.Distance(transform.position, proposed); transform.position = proposed;
            HasPulsed |= Pulse;
        }
    }
}
