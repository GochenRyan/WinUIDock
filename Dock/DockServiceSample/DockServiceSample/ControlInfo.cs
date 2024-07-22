using Microsoft.UI.Xaml.Controls;

namespace DockServiceSample
{
    public class ControlInfo
    {
        public ControlInfo(string name, StandardControlGroup group)
        {
            m_name = name;
            m_group = group;
        }

        public Control Control { get; set; }
        public string Name => m_name;
        public StandardControlGroup Group => m_group;

        private string m_name;
        private StandardControlGroup m_group;
    }
}
