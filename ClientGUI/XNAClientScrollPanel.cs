using System.Collections.Generic;
using System.Linq;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;

namespace ClientGUI
{
    /// <summary>
    /// A scroll panel that can be declared from INI layouts.
    /// </summary>
    /// <remarks>
    /// <see cref="XNAScrollPanel"/> scrolls only the controls inside its inner
    /// content panel, but the INI system ($CC keys) attaches child controls
    /// directly to the declared control, where they would sit beside the content
    /// panel and never scroll. This subclass re-homes INI-declared children into
    /// the content panel once the panel has composed itself, and routes any
    /// controls added later to the same place, so a section can be turned
    /// scrollable by changing its control type alone.
    /// </remarks>
    public class XNAClientScrollPanel : XNAScrollPanel
    {
        private bool composed = false;

        public XNAClientScrollPanel(WindowManager windowManager) : base(windowManager)
        {
        }

        public override void Initialize()
        {
            // Children declared in INI are added (and initialized) before this
            // control's own Initialize runs, while the content panel is not yet
            // composed. Capture them now so they can be re-homed afterwards.
            List<XNAControl> preInitChildren = Children.ToList();

            base.Initialize();

            // The content panel's ChildAdded handlers are hooked by now, so these
            // adds register with content-size tracking and make the panel scroll.
            // AddChildWithoutInitialize is used because the INI system has already
            // initialized these controls, and initializing twice is not safe.
            foreach (XNAControl child in preInitChildren)
            {
                RemoveChild(child);
                ContentPanel.AddChildWithoutInitialize(child);
            }

            composed = true;
        }

        public override void AddChild(XNAControl child)
        {
            if (composed)
                ContentPanel.AddChild(child);
            else
                base.AddChild(child);
        }
    }
}
