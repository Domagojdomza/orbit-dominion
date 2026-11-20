using UnityEngine;
using UnityEngine.UIElements;

namespace OrbitDominion
{
    [DefaultExecutionOrder(500)]
    public sealed class SurfaceHud : MonoBehaviour
    {
        PrototypeBootstrap world;
        VisualElement root, panel, aimDot;
        Label title, biome, signal, total, message, controls, objective, suit, reticle, command;
        readonly System.Collections.Generic.List<Label> contacts = new();
        bool visible;

        public void Initialize(PrototypeBootstrap bootstrap, VisualElement hudRoot)
        {
            world = bootstrap;
            root = hudRoot;
            panel = new VisualElement(); panel.AddToClassList("surface-hud"); root.Add(panel);
            title = Add("SURFACE EXPEDITION", "surface-title");
            biome = Add("", "surface-subtitle");
            signal = Add("", "surface-signal");
            total = Add("", "surface-total");
            message = Add("", "surface-message");
            controls = Add("W/S DRIVE · A/D STEER · SHIFT BOOST · B BUILD · T RALLY · I SCAN · F RECOVER · L LAUNCH", "surface-controls");
            objective = Add("", "surface-objective");
            suit = Add("", "surface-suit");
            reticle = Add("+", "ground-reticle");
            aimDot = new VisualElement { pickingMode = PickingMode.Ignore };
            aimDot.AddToClassList("ground-aim-dot"); panel.Add(aimDot);
            command = Add("", "ground-marker");
            panel.style.display = DisplayStyle.None;
        }

        Label Add(string value, string style)
        {
            var label = new Label(value); label.AddToClassList(style); label.pickingMode = PickingMode.Ignore; panel.Add(label); return label;
        }

        void Update()
        {
            if (world == null || root == null) return;
            bool active = world.Surface.Active;
            if (visible != active)
            {
                visible = active;
                foreach (var child in root.Children()) if (child != panel) child.style.visibility = active ? Visibility.Hidden : Visibility.Visible;
                panel.style.display = active ? DisplayStyle.Flex : DisplayStyle.None;
            }
            if (!active) return;
            bool onFoot = world.Surface.OnFoot;
            title.text = world.Surface.PlanetName + (onFoot ? " / ON FOOT" : " / ROVER EXPEDITION");
            biome.text = world.Surface.Biome.ToString().ToUpperInvariant() + " BIOME  ·  " +
                (onFoot ? "WAYFARER EXPLORER" : world.Surface.RoverAfloat ? "AMPHIBIOUS ROVER" : "SIX-WHEEL ROVER") +
                $"  ·  {(onFoot ? world.Surface.WalkingSpeed : Mathf.Abs(world.Surface.RoverSpeed)) * 3.6f:0} KM/H";
            signal.text = world.Surface.InteractionHint + "\n" + world.Surface.SignalHint;
            if (!onFoot) signal.text += "\n" + world.Surface.ActivityHint;
            total.text = $"RELICS {world.Surface.RelicsFound}  ·  BUILT MODULES {world.Surface.LocalBuildCount}";
            objective.text = world.Surface.GroundObjective;
            var vital = world.Surface.GroundBattle.Suit;
            suit.style.display = onFoot ? DisplayStyle.Flex : DisplayStyle.None;
            suit.text = $"SHIELD {vital.Shield:0}/60   HEALTH {vital.Hull:0}/100\n" + world.Surface.GroundWeaponHint;
            suit.style.color = world.Surface.UnderFire ? new Color(1,.32f,.2f) : new Color(.8f,.94f,1);
            reticle.style.display = onFoot && !world.Surface.BuildMode && !world.Player.Paused && !world.Surface.Recovering ? DisplayStyle.Flex : DisplayStyle.None;
            reticle.text = world.Surface.GroundAimBlocked || world.Surface.HitConfirmed ? "×" : world.Surface.Aiming ? "" : "+";
            aimDot.style.display = world.Surface.Aiming && !world.Player.Paused && !world.Surface.Recovering &&
                !world.Surface.GroundAimBlocked && !world.Surface.HitConfirmed ? DisplayStyle.Flex : DisplayStyle.None;
            aimDot.style.backgroundColor = world.Surface.GroundAimHostile ? new Color(1,.38f,.2f) : Color.white;
            reticle.style.color = world.Surface.GroundAimBlocked ? new Color(1,.7f,.2f) :
                world.Surface.HitConfirmed || world.Surface.GroundAimHostile ? new Color(1,.38f,.2f) : Color.white;
            if(world.Surface.GroundAimBlocked) suit.text += "\nMUZZLE BLOCKED / Step clear of cover";
            UpdateMarkers();
            message.text = world.Player.Paused ? "EXPEDITION PAUSED / ESC to resume" : world.Surface.Message;
            controls.text = world.Player.Paused ? "ESC / RESUME EXPEDITION" :
                world.Surface.BuildMode ? world.Surface.BuildHint :
                onFoot ? "WASD MOVE · SHIFT RUN · RMB AIM · LMB FIRE · R RECHARGE · E BOARD · G CAPTURE · B BUILD · I SCAN · F RECOVER · ESC PAUSE" :
                "W/S DRIVE · A/D STEER · SHIFT BOOST · E EXIT · B BUILD · T RALLY · I SCAN · F RECOVER · F5 SAVE · L LAUNCH · ESC PAUSE";
        }

        void UpdateMarkers()
        {
            var surface = world.Surface; var actors = surface.GroundContacts;
            while (contacts.Count < actors.Count) contacts.Add(Add("", "ground-marker"));
            for (int i = 0; i < contacts.Count; i++)
            {
                bool show = i < actors.Count && actors[i].vitals.Alive && !world.Player.Paused;
                if (show && !actors[i].hostile && !actors[i].training)
                {
                    var actorRoot = actors[i].root;
                    var eye = actorRoot.parent.InverseTransformPoint(Camera.main.transform.position);
                    show = surface.Aiming && Vector3.Distance(surface.ExplorerLocal, actorRoot.localPosition) < 18 &&
                        surface.TraceGroundCover(eye, actorRoot.localPosition + Vector3.up * 1.6f) >= .99f;
                }
                contacts[i].style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
                if (!show) continue;
                var actor = actors[i]; var marker = contacts[i];
                float distance = Vector3.Distance(surface.ExplorerLocal, actor.root.localPosition);
                marker.text = actor.title + $" · {distance:0} m" + (actor.hostile ? $"\n{(actor.alert >= .95f ? "FIRING" : actor.alert > .1f ? "ALERT" : "PATROL")} · {actor.vitals.Hull + actor.vitals.Shield:0} HP" : "");
                marker.style.color = actor.hostile ? new Color(1,.39f,.26f) : actor.training ? new Color(1,.72f,.25f) : new Color(.35f,.95f,.75f);
                PlaceMarker(marker, actor.root.position + Vector3.up * 2.4f);
            }
            bool capture = surface.Active && world.Empire.State.Get(surface.PlanetId).faction == 3 && !world.Player.Paused;
            command.style.display = capture ? DisplayStyle.Flex : DisplayStyle.None;
            if (capture)
            {
                command.text = $"COMMAND TERMINAL · {Vector3.Distance(surface.ExplorerLocal,surface.CommandLocal):0} m\n" +
                    (surface.GroundDefenders > 0 ? "CLEAR GUARDS FIRST" : $"HOLD G · {surface.GroundCaptureProgress*100:0}%");
                command.style.color = new Color(.35f,.85f,1);
                var first = actors.Count > 0 ? actors[0].root.parent : null;
                if (first != null) PlaceMarker(command, first.TransformPoint(surface.CommandLocal + Vector3.up * 2));
            }
        }

        void PlaceMarker(Label marker, Vector3 position)
        {
            Vector3 p = Camera.main.WorldToViewportPoint(position);
            bool behind = p.z < 0;
            if (behind) { p.x = 1 - p.x; p.y = 1 - p.y; marker.text = "< " + marker.text; }
            marker.style.left = Mathf.Clamp(p.x * root.resolvedStyle.width - 95, 16, root.resolvedStyle.width - 220);
            marker.style.top = Mathf.Clamp((1 - p.y) * root.resolvedStyle.height - 36, 250, root.resolvedStyle.height - 210);
        }
    }
}
