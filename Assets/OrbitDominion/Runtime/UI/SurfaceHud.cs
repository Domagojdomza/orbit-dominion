using UnityEngine;
using UnityEngine.UIElements;

namespace OrbitDominion
{
    [DefaultExecutionOrder(500)]
    public sealed class SurfaceHud : MonoBehaviour
    {
        PrototypeBootstrap world;
        VisualElement root, panel;
        Label title, biome, signal, total, message, controls;
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
            title.text = world.Surface.PlanetName + " / ROVER EXPEDITION";
            biome.text = world.Surface.Biome.ToString().ToUpperInvariant() + " BIOME  ·  " +
                (world.Surface.RoverAfloat ? "AMPHIBIOUS ROVER" : "SIX-WHEEL ROVER") +
                $"  ·  {Mathf.Abs(world.Surface.RoverSpeed) * 3.6f:0} KM/H";
            signal.text = world.Surface.ActivityHint + "\n" + world.Surface.SignalHint;
            total.text = $"RELICS {world.Surface.RelicsFound}  ·  BUILT MODULES {world.Surface.LocalBuildCount}";
            message.text = world.Player.Paused ? "EXPEDITION PAUSED / ESC to resume" : world.Surface.Message;
            controls.text = world.Player.Paused ? "ESC / RESUME EXPEDITION" :
                world.Surface.BuildMode ? world.Surface.BuildHint :
                "W/S DRIVE · A/D STEER · SHIFT BOOST · B BUILD · T RALLY · I SCAN · F RECOVER · F5 SAVE · L LAUNCH · ESC PAUSE";
        }
    }
}
