using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Avalonia.Data;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.VisualTree;

namespace AvRichTextBox;

public partial class RichTextBox
{

#if DEBUG
    //VISUAL DEBUGGER -Panel for visualization of runs. Only created in Debug mode, default hidden but can be shown by: ShowDebuggerPanelInDebugMode=true
    private DebuggerPanel debuggerPanel = null!;
    private void ToggleDebuggerPanel(bool visible) { debuggerPanel?.IsVisible = visible; }

    private void BindSelectionPropertiesToDebuggerPanel()
    {
        if (ShowDebuggerPanelInDebugMode)
        {
            //Create Debugger Panel only in debug mode and if shown
            debuggerPanel = new() { Width = 400, DataContext = FlowDoc };
            DockPanel.SetDock(debuggerPanel, Dock.Right);
            MainDP.Children.Insert(0, debuggerPanel);
            debuggerPanel.DataContext = RtbVm;
            debuggerPanel.Bind(Visual.IsVisibleProperty, new Binding("RunDebuggerVisible"));
            debuggerPanel.SelEndTB.Bind(TextBlock.TextProperty, new Binding("FlowDoc.Selection.End") { StringFormat = "DocSelEnd={0}" });
            debuggerPanel.SelStartTB.Bind(TextBlock.TextProperty, new Binding("FlowDoc.Selection.Start") { StringFormat = "DocSelStart={0}" });
            debuggerPanel.BiasForwardEndTB.Bind(TextBlock.TextProperty, new Binding("FlowDoc.Selection.BiasForwardEnd") { StringFormat = "BiasForwardEnd={0}" });
            debuggerPanel.BiasForwardStartTB.Bind(TextBlock.TextProperty, new Binding("FlowDoc.Selection.BiasForwardStart") { StringFormat = "BiasForwardStart={0}" });
            debuggerPanel.ParagraphsLB.ItemsSource = FlowDoc.SelectionParagraphs;
            RtbVm.RunDebuggerVisible = ShowDebuggerPanelInDebugMode;
            this.Width += (RtbVm.RunDebuggerVisible ? 400 : 0);
            FlowDoc.ShowDebugger = RtbVm.RunDebuggerVisible;


        }
    }

#endif

}
