using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.Reflection;
using System.Text;
using System.Windows.Forms;
using System.Xml;
using XService.Utils;

namespace XService.UI
{
    public class UiTools
    {
        /// <summary>All trace logging of XService.UI assmebly should use this TraceSwitch</summary>
        public static TraceSwitch TrcLvl = new TraceSwitch("XService.UI", "XService.UI");

        /// <summary>Render HTLM-style formated text into RichTextBox. Note: it supports only limited set of HTML tags - <br>, <p>, <color value=...">, <b>, <i>, <u></summary>
        /// <param name="pTarget">Target RichTextBox</param>
        /// <param name="pText">HTLM-style formated text</param>
        public static void RenderInto(System.Windows.Forms.RichTextBox pTarget, string pText)
        {
            XmlDocument dom = new XmlDocument();
            try { dom.LoadXml(pText); }
            catch { pText = "<Text>" + pText + "</Text>"; }
            dom.LoadXml(pText);

            XmlNode attr = dom.DocumentElement.GetAttributeNode("cleanup");
            if (attr != null)
            {
                if (StrUtils.GetAsBool(attr.Value.Trim())) // Convert.ToBoolean(attr.Value.Trim()))
                    pTarget.Clear();
            }

            renderDomNodeInto(pTarget, dom.DocumentElement);
        }

        /// <summary>Collect all TabPage objects from TabControl into specified list</summary>
        /// <param name="pContainer">TabControl object to collect tab pages from</param>
        /// <param name="pList">List where to save TabPage objects</param>
        public static void PagesToList(TabControl pContainer, List<TabPage> pList)
        {
            foreach (TabPage page in pContainer.TabPages)
            {
                if (!pList.Contains(page))
                    pList.Add(page);
            }
        }

        /// <summary>Hide all tab pages except specified</summary>
        /// <param name="pContainer">TabControl object where to hide or show tab pages</param>
        /// <param name="pPages">List of all tab pages (previously collected)</param>
        /// <param name="pPage">TagPage only to show, if null then all tab pages will be hidden</param>
        public static void HideAllPagesBut(TabControl pContainer, List<TabPage> pPages, TabPage pPage)
        {
            for (int i = 0; i < pPages.Count; i++)
            {
                TabPage page = pPages[i];
                if (pPage != null && page.Equals(pPage))
                {
                    if (!pContainer.TabPages.Contains(page))
                        pContainer.TabPages.Add(page);
                }
                else
                {
                    if (pContainer.TabPages.Contains(page))
                        pContainer.TabPages.Remove(page);
                }
            }
        }

        /// <summary>Return parent Form of specified Control</summary>
        /// <param name="pControl">Control to find parent Form for</param>
        /// <returns>Returns parent Form of specified Control or null when not found</returns>
        public static Form ParentFormOf(Control pControl)
        {
            while (pControl != null && !(pControl is Form))
            {
                pControl = pControl.Parent;
            }
            if (pControl is Form)
                return (Form)pControl;
            return null;
        }

        /// <summary>Serialize Form view parameters into a string</summary>
        /// <param name="pForm">Form to serialize view parameter for</param>
        /// <returns>String in LOP-format with Form view parameters</returns>
        public static string SerializeFormView(Form pForm)
        {
            string result = string.Format("X:{0}; Y:{1}; W:{2}; H:{3}; State:{4};", pForm.Left, pForm.Top, pForm.Width, pForm.Height, pForm.WindowState);
            Trace.WriteLineIf(TrcLvl.TraceWarning, TrcLvl.TraceWarning ? string.Format(" * {0}.SerializeFormView: {1}", pForm.Name, result) : "");
            return result;
        }

        public static string SerializeFormView(Form pForm, string pExtraProps)
        {
            string result = string.Format("X:{0}; Y:{1}; W:{2}; H:{3}; State:{4}; {5}", pForm.Left, pForm.Top, pForm.Width, pForm.Height, pForm.WindowState,
                (pExtraProps != null ? pExtraProps : ""));
            Trace.WriteLineIf(TrcLvl.TraceWarning, TrcLvl.TraceWarning ? string.Format(" * {0}.SerializeFormView: {1}", pForm.Name, result) : "");
            return result;
        }

        public delegate void CustomAttributeDeserializer(string pName, string pValue);
        public delegate void DeserializeCustomUiMethod(Dictionary<string, string> pViewProps, Form pForm);

        public static void FixBounds(ref Rectangle R, Form pForm, Rectangle pScreen)
        {
            Rectangle origR = R;
            string chg = "";

            // if Width/Height is less than allowed minimal Width/Height 
            if (R.Width < pForm.MinimumSize.Width)
            {
                R.Width = pForm.MinimumSize.Width;
                chg += string.Format("W={0},", R.Width);
            }
            if (R.Height < pForm.MinimumSize.Height)
            {
                R.Height = pForm.MinimumSize.Height;
                chg += string.Format("H={0},", R.Height);
            }

            // if Width/Height is higher than Screen Width/Height 
            if (R.Width > pScreen.Width)
            {
                R.Width = pScreen.Width;
                chg += string.Format("W={0},", R.Width);
            }
            if (R.Height > pScreen.Height)
            {
                R.Height = pScreen.Height;
                chg += string.Format("H={0},", R.Height);
            }

            // X cannot be less than 1/N part of form width
            if (R.X < 0)
            {
                int range = (R.Width / 3);
                if (Math.Abs(R.X) > range)
                {
                    R.X = -1 * range;
                    chg += string.Format("X={0},", R.X);
                }
            }
            // X cannot be higher than (ScreenWidth - 1/N) part of form width
            int maxX = pScreen.Width - (R.Width / 2);
            if (R.X > maxX)
            {
                R.X = maxX;
                chg += string.Format("X={0},", R.X);
            }

            // Y cannot be less than 0
            if (R.Y < 0)
            {
                R.Y = 0;
                chg += string.Format("Y={0},", R.Y);
            }
            // Y cannot be higher than (ScreenHeight - 1/N) part of form height
            int maxY = pScreen.Height - (R.Height / 2);
            if (R.Y > maxY)
            {
                R.Y = maxY;
                chg += string.Format("Y={0},", R.Y);
            }

            if (chg.Length > 0)
                chg = chg.TrimEnd(",".ToCharArray());

            Trace.WriteLineIf(TrcLvl.TraceWarning, TrcLvl.TraceWarning ? String.Format(
                "!FixBounds( {0} => {1}; changes:{2} )", origR, R, chg) : "");
        }

        /// <summary>Deserialize Form view parameters from a string</summary>
        /// <param name="pViewInfo">String in LOP-format with Form view parameters to deserialize</param>
        /// <param name="pForm">Form to initialize view parameters for</param>
        public static void DeserializeFormView(string pViewInfo, Form pForm)
        {
            PerformFormViewDeserialization(pViewInfo, pForm, null, null);
        }

        /// <summary>Deserialize Form view parameters from a string</summary>
        /// <param name="pViewInfo">String in LOP-format with Form view parameters to deserialize</param>
        /// <param name="pForm">Form to initialize view parameters for</param>
        /// <param name="pDeserializer1">Delegate to deserialize attributes which were not recognized</param>
        public static void DeserializeFormView(string pViewInfo, Form pForm, CustomAttributeDeserializer pDeserializer1)
        {
            PerformFormViewDeserialization(pViewInfo, pForm, pDeserializer1, null);
        }

        /// <summary>Deserialize Form view parameters from a string</summary>
        /// <param name="pViewInfo">String in LOP-format with Form view parameters to deserialize</param>
        /// <param name="pForm">Form to initialize view parameters for</param>
        /// <param name="pDeserializer2">Delegate to deserialize attributes which were not recognized</param>
        public static void DeserializeFormView(string pViewInfo, Form pForm, DeserializeCustomUiMethod pDeserializer2)
        {
            PerformFormViewDeserialization(pViewInfo, pForm, null, pDeserializer2);
        }

        public static void PerformFormViewDeserialization(string pViewInfo, Form pForm, CustomAttributeDeserializer pDeserializer1, DeserializeCustomUiMethod pDeserializer2)        
        {
            Trace.WriteLineIf(TrcLvl.TraceWarning, TrcLvl.TraceWarning ? String.Format(
                "DeserializeFormView( {0} => [{1}] )", pViewInfo, string.Format("{0}:{1}", pForm.Name, pForm.GetType())) : "");

            Dictionary<string, string> prms = new Dictionary<string, string>();

            CollectionUtils.ParseParametersStrEx(prms, pViewInfo, true, ';', ":=");

            // Note: trying to resolve issue with wrong visual gabarites (outsize of screen)

            int n;
            string s;
            int rx = pForm.Left, ry = pForm.Top;
            int rw = pForm.Width, rh = pForm.Height;

            // need to read W,H first becuase them are only limited by screen size
            if (prms.TryGetValue("w", out s))
            {
                if (StrUtils.GetAsInt(s, out n)) rw = n; // pForm.Width = FixSize(n, pForm.MinimumSize.Width, Screen.PrimaryScreen.WorkingArea.Width);
            }
            if (prms.TryGetValue("h", out s))
            {
                if (StrUtils.GetAsInt(s, out n)) rh = n; // pForm.Height = FixSize(n, pForm.MinimumSize.Height, Screen.PrimaryScreen.WorkingArea.Height);
            }
            Trace.WriteLineIf(TrcLvl.TraceWarning, TrcLvl.TraceWarning ? String.Format(
                " * DeserializeFormView: W={0}; H={1}", pForm.Width, pForm.Height) : "");

            // then we need to read X,Y becuase their correction also may depends on W,H
            if (prms.TryGetValue("x", out s))
            {
                if (StrUtils.GetAsInt(s, out n)) rx = n; // pForm.Left = FixLocation(n, pForm.Width, Screen.PrimaryScreen.WorkingArea.Width);
            }
            if (prms.TryGetValue("y", out s))
            {
                if (StrUtils.GetAsInt(s, out n)) ry = n; // pForm.Top = FixLocation(n, pForm.Height, Screen.PrimaryScreen.WorkingArea.Height);
            }

            Rectangle R = new Rectangle(rx, ry, rw, rh);
            FixBounds(ref R, pForm, Screen.PrimaryScreen.WorkingArea);

            if (pForm.Left != R.Left) pForm.Left = R.Left;
            if (pForm.Top != R.Top) pForm.Top = R.Top;
            if (pForm.Width != R.Width) pForm.Width = R.Width;
            if (pForm.Height != R.Height) pForm.Height = R.Height;

            Trace.WriteLineIf(TrcLvl.TraceWarning, TrcLvl.TraceWarning ? String.Format(
                " * DeserializeFormView: fixed( location=[{0}; {1};], size[{2}; {3}] )", pForm.Left, pForm.Top, pForm.Width, pForm.Height) : "");

            foreach (KeyValuePair<string, string> prm in prms)
            {
                if (prm.Key == "x" || prm.Key == "y" || prm.Key == "w" || prm.Key == "h")
                { 
                    // nothing to do here! these already deserialized before this loop
                }
                else if (prm.Key == "state")
                {
                    object st;
                    if (StrUtils.GetAsEnumEx(prm.Value, typeof(FormWindowState), out st))
                    {
                        Trace.WriteLineIf(TrcLvl.TraceWarning, TrcLvl.TraceWarning ? String.Format(
                            " * DeserializeFormView: state={0}/{1}", st, (FormWindowState)st) : "");
                        pForm.WindowState = (FormWindowState)st;
                    }
                }
                // all other attributes can be deserialized via special method
                else if (pDeserializer1 != null)
                {
                    pDeserializer1(prm.Key, prm.Value);
                }
            }
            if (pDeserializer2 != null)
                pDeserializer2(prms, pForm);
        }

        /// <summary>Convert string into color value. It could be color name or explicit HTML-style color value (prefixed with '#')</summary>
        public static Color StrToColor(string id)
        {
            bool checkAlpha = true;
            Color result = Color.Black;
            if (id.StartsWith("#"))
            {
                // RGB
                id = id.Remove(0, 1);
                checkAlpha = (id.Length <= 6); // when Alpha fraction is not specified explicitly then need to check it later
                result = Color.FromArgb(Convert.ToInt32(id, 16));
            }
            else if (id.StartsWith("$"))
            {
                // BGR - this is how color present in Borland Delphi
                id = id.Remove(0, 1);
                checkAlpha = (id.Length <= 6); // when Alpha fraction is not specified explicitly then need to check it later
                int n = Convert.ToInt32(id, 16);
                n = ((n & 0xFF0000) >> 16) | (n & 0x00FF00) | ((n & 0x0000FF) << 16);
                result = Color.FromArgb(n);
            }
            else
            {
                result = Color.FromName(id);
                checkAlpha = false;
            }
            if (checkAlpha)
            {
                // check when Alpha==0 - need to reset it to fully non-transparent (255)!
                if (result.A == 0)
                {
                    result = Color.FromArgb(255, result);
                }
            }
            return result;
        }

        private static Dictionary<Color, Brush> cacheOfBrushes = new Dictionary<Color, Brush>();
        public static Brush GetSolidBrush(Color c)
        {
            Brush result;
            lock (cacheOfBrushes)
            {
                if (cacheOfBrushes.TryGetValue(c, out result))
                    return result;

                result = new SolidBrush(c);
                cacheOfBrushes[c] = result;
            }
            return result;
        }

        private static Dictionary<string, Pen> cacheOfPens = new Dictionary<string, Pen>();
        public static Pen GetPen(Color c, float pWidth)
        {
            Pen result;
            lock (cacheOfPens)
            {
                string id = string.Format("{0}", ((uint)c.ToArgb()).ToString("X8"));
                if (pWidth > 0)
                    id += string.Format("-{0}", pWidth);
                if (cacheOfPens.TryGetValue(id, out result))
                    return result;

                result = (pWidth > 0 ? new Pen(c, pWidth) : new Pen(c));
                cacheOfPens[id] = result;
            }
            return result;
        }

        #region CMB: ComboBox

        public static int CMB_IndexOfCI(ComboBox pCmb, string pItem)
        {
            for (int i = 0; i < pCmb.Items.Count; i++)
            {
                if (StrUtils.IsSameText(pItem, pCmb.Items[i].ToString()))
                    return i;
            }
            return -1;
        }

        #endregion // CMB: ComboBox

        #region CTRL: Control

        /// <summary>Enable/disable specified Control, optionally can hide/unhide it</summary>
        /// <param name="pCtrl">Control to enable/disable</param>
        /// <param name="pEnable">New value for Enabled property</param>
        /// <param name="pVisible">Optional. New value for Visible property or null</param>
        public static void CTRL_Enable(Control pCtrl, bool pEnable, bool? pVisible)
        {
            if (pCtrl == null) return;
            pCtrl.Enabled = pEnable;
            if (pVisible.HasValue)
                pCtrl.Visible = pVisible.Value;
        }

        /// <summary>Enable/disable and/or hide/unhide child controls inside specified container</summary>
        /// <param name="pContainer">Container to handle controls in</param>
        /// <param name="pEnabled">New value Enabled property of child controls. Or null to keep as-is</param>
        /// <param name="pVisible">New value for Visible property of child controls. Or null to keep as-is</param>
        /// <param name="pRecursive">If need to scan child-of-child recusrsively</param>
        /// <param name="pTypes">Optional. List of control types to handle only</param>
        public static void ETRL_EnableChilds(Control pContainer, bool? pEnabled, bool? pVisible, bool pRecursive, List<Type> pTypes)
        {
            foreach (Control ctrl in pContainer.Controls)
            {
                bool isMatch = (pTypes == null || (pTypes != null && pTypes.Contains(ctrl.GetType())));
                if (isMatch)
                {
                    if (pEnabled.HasValue)
                        ctrl.Enabled = pEnabled.Value;
                    if (pVisible.HasValue)
                        ctrl.Visible = pVisible.Value;
                }
                if (pRecursive && ctrl.Controls.Count > 0)
                    ETRL_EnableChilds(ctrl, pEnabled, pVisible, pRecursive, pTypes);
            }
        }

        /// <summary>Delegate metdop to be used by child controls iterator</summary>
        public delegate bool ChildControlHandler(Control pCtrl, object pContext);

        /// <summary>Loop over all child controls and call specified delegate</summary>
        /// <param name="pCtrl">Control to loop over childs of</param>
        /// <param name="pHandler">Delegate method to call</param>
        /// <param name="pContext">Context object to pass as parameter</param>
        /// <returns>Returns true when succesfully looped over all childs without stop</returns>
        public static bool ForEachControl(Control pCtrl, ChildControlHandler pHandler, object pContext)
        {
            bool isOk = false;
            foreach (Control ctrl in pCtrl.Controls)
            {
                isOk = pHandler(ctrl, pContext);
                if (!isOk) return isOk;
            }
            foreach (Control ctrl in pCtrl.Controls)
            {
                if (ctrl.Controls.Count > 0)
                {
                    isOk = ForEachControl(ctrl, pHandler, pContext);
                    if (!isOk) return isOk;
                }
            }
            return isOk;
        }

        /// <summary>Find child control of specified type</summary>
        /// <param name="pControl">Where to search Control of specified type</param>
        /// <param name="pCtrlType">Type of </param>
        /// <returns>Returns Control object or null when not found</returns>
        public static object FindChildControlOf(Control pControl, Type pCtrlType)
        {
            foreach (Control ctrl in pControl.Controls)
            {
                if (pCtrlType.Equals(ctrl.GetType()))
                    return ctrl;
            }
            return null;
        }

        #endregion // CTRL: Control

        #region DGV: DataGridView

        /// <summary>Set double-buffering for specified DataGridView</summary>
        /// <param name="pDgv">DataGridView to set double-buffering for</param>
        /// <param name="pValue">Double-buffering value to set</param>
        public static void DGV_SetDoubleBuffering(DataGridView pDgv, bool pValue)
        {
            if (!System.Windows.Forms.SystemInformation.TerminalServerSession)
            {
                Type dgvType = pDgv.GetType();
                PropertyInfo pi = dgvType.GetProperty("DoubleBuffered", BindingFlags.Instance | BindingFlags.NonPublic);
                if (pi != null)
                    pi.SetValue(pDgv, pValue, null);
            }
        }

        /// <summary>Select specified row# in specified DataGridView</summary>
        public static void DGV_SelectRow(DataGridView pDgv, int pRowIndex)
        {
            if (pRowIndex < 0 || pRowIndex >= pDgv.Rows.Count) return;

            pDgv.FirstDisplayedScrollingRowIndex = (pRowIndex > 0 ? pRowIndex - 1 : pRowIndex);
            pDgv.Rows[pRowIndex].Selected = true;
        }

        /// <summary>Serialize columns of DataGridView into a semicolon-separated list, where each list item is {ColumnName}={ColunmWidth}</summary>
        /// <param name="pDgv">DataGridView to serialize columns of</param>
        public static string DGV_SerializeColumns(DataGridView pDgv)
        {
            string result = "";
            foreach (DataGridViewColumn dgvc in pDgv.Columns)
            {
                if (!dgvc.Visible) continue;
                result += string.Format("{0}={1}; ", dgvc.Name, dgvc.Width);
            }
            return result.TrimEnd();
        }

        /// <summary>Deserialize a semicolon-separated list into columns of DataGridView, where each list item is {ColumnName}={ColunmWidth}</summary>
        /// <param name="pDgv">DataGridView to deserialize columns for</param>
        public static void DGV_DeserializeColumns(DataGridView pDgv, string pDefs)
        {
            Dictionary<string, string> props = new Dictionary<string, string>();
            StrUtils.ParseLop(pDefs, props, true);
            foreach (DataGridViewColumn dgvc in pDgv.Columns)
            {
                if (!dgvc.Visible) continue;
                string s;
                if (props.TryGetValue(dgvc.Name.ToLower(), out s))
                    dgvc.Width = Convert.ToInt32(s);
            }
        }

        /// <summary>Concatenate content of DataGridView control into 1 string</summary>
        /// <param name="pDgv">DataGridView control to concatenate content of it</param>
        /// <param name="pIncludeHeader">Flag. If to include column captions as 1st line</param>
        /// <param name="pCellsDelimiter">Delimiter for cells. If null it will use current ListSeparator</param>
        /// <param name="pRowsDelimiter">Delimiter for rows. If null it will use Environment.NewLine</param>
        /// <returns>String which is result of DataGridView content concatenation</returns>
        public static string DGV_ToString(DataGridView pDgv, bool pIncludeHeader, string pCellsDelimiter, string pRowsDelimiter)
        { 
            return DGV_ToStringEx(pDgv, pIncludeHeader, pCellsDelimiter, pRowsDelimiter, false);
        }

        /// <summary>Concatenate content of DataGridView control into 1 string</summary>
        /// <param name="pDgv">DataGridView control to concatenate content of it</param>
        /// <param name="pIncludeHeader">Flag. If to include column captions as 1st line</param>
        /// <param name="pCellsDelimiter">Delimiter for cells. If null it will use current ListSeparator</param>
        /// <param name="pRowsDelimiter">Delimiter for rows. If null it will use Environment.NewLine</param>
        /// <param name="pCsvFormat">If need to use pure CSV format (enclosed values with double quotes when they contains special charactars)</param>
        /// <returns>String which is result of DataGridView content concatenation</returns>
        public static string DGV_ToStringEx(DataGridView pDgv, bool pIncludeHeader, string pCellsDelimiter, string pRowsDelimiter, bool pCsvFormat)
        {
            if (pCellsDelimiter == null)
                pCellsDelimiter = CultureInfo.CurrentCulture.TextInfo.ListSeparator;
            if (pRowsDelimiter == null)
                pRowsDelimiter = Environment.NewLine;

            StringBuilder sb = new StringBuilder(100 * pDgv.Rows.Count * pDgv.Columns.Count);
            if (pIncludeHeader)
            {
                foreach (DataGridViewColumn dgc in pDgv.Columns)
                    sb.Append(dgc.HeaderText + pCellsDelimiter);
                sb.Append(pRowsDelimiter);
            }
            foreach (DataGridViewRow dgr in pDgv.Rows)
            {
                foreach (DataGridViewColumn dgc in pDgv.Columns)
                {
                    string v = dgr.Cells[dgc.Index].Value.ToString();
                    if (pCsvFormat && v.IndexOfAny(StrUtils.CH_QUOTABLE_CHARS) >= 0)
                        v = StrUtils.AnsiQuotedStr(v, '\"');
                    sb.Append(v);
                    sb.Append(pCellsDelimiter);
                }
                sb.Append(pRowsDelimiter);
            }
            return sb.ToString();
        }

        /// <summary>Concatenate content of DataGridView into string and put into Clipboard</summary>
        public static void DGV_ToClipboard(DataGridView dgv)
        {
            string data = DGV_ToString(dgv, true, "\t", null);
            try { Clipboard.SetDataObject(data, true, 3, 330); }
            catch { }
        }

        /// <summary>Hide (or unhide) specified columns in specified DataGridView control</summary>
        /// <param name="pDgv">DataGridView control to hide (or unhide) columns for</param>
        /// <param name="pColumnsToHide">List of columns to hide. Columns can be identified by ColumnName or by HeaderText. When null it will unhide all hidden columns</param>
        /// <returns>Returns number of columns which visibility state was changed by this method</returns>
        public static int DGV_HideColumns(DataGridView pDgv, string pColumnsToHide)
        {
            int result = 0;
            if (string.IsNullOrEmpty(pColumnsToHide))
            {
                foreach (DataGridViewColumn dgc in pDgv.Columns)
                {
                    if (!dgc.Visible)
                    {
                        dgc.Visible = true;
                        result++;
                    }
                }
                return result; 
            }

            pColumnsToHide = pColumnsToHide.Replace(";", ",");
            pColumnsToHide = ("," + pColumnsToHide + ",").ToUpper();
            foreach (DataGridViewColumn dgc in pDgv.Columns)
            {
                string id1 = ("," + dgc.Name + ",").ToUpper();
                string id2 = ("," + dgc.HeaderText + ",").ToUpper();
                bool needToHide = (pColumnsToHide.IndexOf(id1) >= 0 || pColumnsToHide.IndexOf(id2) >= 0);
                if (needToHide)
                {
                    dgc.Visible = false;
                    result++;
                }
            }
            return result; 
        }

        [Flags]
        public enum EDvgColumnDumpFlags
        {
            None = 0,
            UseCaptions = 0x0001, // otherwise - useNames
            IncludeCaption = 0x0002, // use Name+Caption
            OnlyVisible = 0x0004, // OnlyVisible + OnlyHidden = Show All
            OnlyHidden = 0x0008,
            All = OnlyVisible | OnlyHidden,
            IncludeIndex = 0x0010,
            IncludeDisplayIndex = 0x0020,
            IncludeIndexes = IncludeIndex | IncludeDisplayIndex,
            IncludeWidth = 0x0100,

            DefaultDump = All | IncludeCaption,
        }

        /// <summary>
        /// Dump columnd of DataGridView into a string. Could be useful for debug purposes. 
        /// Hidden columns marked with '~'. Column indexes added after '/'. Column display indexes added after '/$'.
        /// Format is {visibilityMarker}{ColumnName}[{HeaderText}]={width}/{index}/${displayIndex}
        /// </summary>
        /// <param name="pView">DataGridView control</param>
        /// <param name="pFlags">Flags defining what to dump</param>
        /// <returns>String which is result of columns list dumping</returns>
        public static string DVG_ColumnAsDump(DataGridView pView, EDvgColumnDumpFlags pFlags)
        {
            bool useCaptions = ((pFlags & EDvgColumnDumpFlags.UseCaptions) != 0);
            bool includeCaption = ((pFlags & EDvgColumnDumpFlags.IncludeCaption) != 0);
            bool onlyVisible = ((pFlags & EDvgColumnDumpFlags.OnlyVisible) != 0);
            bool onlyHidden = ((pFlags & EDvgColumnDumpFlags.OnlyHidden) != 0);
            bool includeIndex = ((pFlags & EDvgColumnDumpFlags.IncludeIndex) != 0);
            bool includeDisplayIndex = ((pFlags & EDvgColumnDumpFlags.IncludeDisplayIndex) != 0);
            bool includeWidth = ((pFlags & EDvgColumnDumpFlags.IncludeWidth) != 0);

            string result = null;
            foreach (DataGridViewColumn dgvc in pView.Columns)
            {
                bool isInclude = (
                    (onlyHidden && !dgvc.Visible)
                    || (onlyVisible && dgvc.Visible)
                    );
                if (!isInclude) continue;

                string item = (dgvc.Visible ? "" : "~"); // mark hidden columns wih '~'
                if (includeCaption)
                    item += string.Format("{0}[{1}]", dgvc.Name, dgvc.HeaderText);
                else
                    item += (useCaptions ? dgvc.HeaderText : dgvc.Name);

                string extra = "";
                if (includeWidth) extra += ((string.IsNullOrEmpty(extra) ? "" : "/") + dgvc.Width.ToString());
                if (includeIndex) extra += ((string.IsNullOrEmpty(extra) ? "" : "/") + dgvc.Index.ToString());
                if (includeDisplayIndex) extra += ((string.IsNullOrEmpty(extra) ? "" : "/$") + dgvc.DisplayIndex.ToString());
                if (!string.IsNullOrEmpty(extra))
                    item += ("=" + extra);

                if (result == null) result = item;
                else result += (";" + item);
            }
            return result;
        }

        /// <summary>Search column by Name or by HeaderText</summary>
        /// <param name="pView">DataGridView control to search column in</param>
        /// <param name="pName">Name of column to search. When null it is ignored</param>
        /// <param name="pHeaderText">HeaderText of column to search. When null it is ignored</param>
        /// <returns>Index of column it found or -1 when not found</returns>
        public static int DVG_IndexOfColumn(DataGridView pView, string pName, string pHeaderText)
        {
            for (int i = 0; i < pView.Columns.Count; i++)
            {
                DataGridViewColumn dgvc = pView.Columns[i];

                bool isMatch = (
                    (pName == null || (pName != null && StrUtils.IsSameText(dgvc.Name, pName)))
                    && (pHeaderText == null || (pHeaderText != null && StrUtils.IsSameText(dgvc.HeaderText, pHeaderText))));

                if (isMatch)
                    return i; // dgvc.Index;
            }
            return -1;
        }

        #endregion // DGV: DataGridView

        #region LAB: Label

        private static Brush vertLab_brLabBk = null;
        private static Brush vertLab_brLabFr = null;
        private static Pen vertLab_pnLabFr = null;
        //private static SizeF vertLab_txtSz = SizeF.Empty;
        private static StringFormat vertLab_drwFmt = new System.Drawing.StringFormat() { FormatFlags = StringFormatFlags.DirectionVertical };

        public class VetricalLabelCtx
        {
            public Brush VertLab_brLabBk = null;
            public Brush VertLab_brLabFr = null;
            public Pen VertLab_pnLabFr = null;
            public SizeF VertLab_txtSz = SizeF.Empty;
            public StringFormat VertLab_drwFmt = new System.Drawing.StringFormat() { FormatFlags = StringFormatFlags.DirectionVertical };
        }

        public static void LAB_DrawVerticalString(Label lab, PaintEventArgs e, string pText)
        {
            Graphics g = e.Graphics;

            if (vertLab_brLabBk == null)
                vertLab_brLabBk = new SolidBrush(SystemColors.Info);
            if (vertLab_brLabFr == null)
                vertLab_brLabFr = new SolidBrush(SystemColors.WindowText);
            if (vertLab_pnLabFr == null)
                vertLab_pnLabFr = new Pen(SystemColors.WindowText);

            SizeF vertLab_txtSz = g.MeasureString(pText, lab.Font);

            g.FillRectangle(vertLab_brLabBk, lab.Bounds);
            g.DrawRectangle(vertLab_pnLabFr, new Rectangle(0, 0, lab.Width - 1, lab.Height - 1));

            PointF pnt = new PointF((lab.Width - vertLab_txtSz.Height) / 2, (lab.Height - vertLab_txtSz.Width) / 2);
            g.DrawString(pText, lab.Font, vertLab_brLabFr, pnt, vertLab_drwFmt);
        }

        public static void LAB_DrawVerticalString(Label lab, PaintEventArgs e, string pText, VetricalLabelCtx pCtx)
        {
            Graphics g = e.Graphics;

            if (pCtx.VertLab_brLabBk == null)
                pCtx.VertLab_brLabBk = new SolidBrush(lab.BackColor); // SystemColors.Info);            
            if (pCtx.VertLab_brLabFr == null)
                pCtx.VertLab_brLabFr = new SolidBrush(lab.ForeColor); //SystemColors.WindowText);
            if (pCtx.VertLab_pnLabFr == null)
                pCtx.VertLab_pnLabFr = new Pen(lab.ForeColor); //SystemColors.WindowText);

            pCtx.VertLab_txtSz = g.MeasureString(pText, lab.Font);

            g.FillRectangle(pCtx.VertLab_brLabBk, lab.Bounds);
            g.DrawRectangle(pCtx.VertLab_pnLabFr, new Rectangle(0, 0, lab.Width - 1, lab.Height - 1));

            PointF pnt = new PointF((lab.Width - pCtx.VertLab_txtSz.Height) / 2, (lab.Height - pCtx.VertLab_txtSz.Width) / 2);
            g.DrawString(pText, lab.Font, pCtx.VertLab_brLabFr, pnt, pCtx.VertLab_drwFmt);
        }


        #endregion // LAB: Label

        #region LV: ListView

        /// <summary>Find ListViewItem with specified Tag</summary>
        /// <param name="pList">ListView to search ListViewItem in</param>
        /// <param name="pTag">Tag to search for</param>
        /// <returns>Index of ListViewItem found or -1 when not found</returns>
        public static int LV_IndexOfTag(ListView pList, object pTag)
        {
            foreach (ListViewItem li in pList.Items)
            {
                if (li.Tag == pTag)
                    return li.Index;
            }
            return -1;
        }

        /// <summary>Serialize content of ListView into string</summary>
        /// <param name="pView">ListView to copy content from</param>
        /// <param name="pIncludeHeader">If need to include header (column names)</param>
        /// <returns>Content of ListView in form of string</returns>
        public static string LV_ToString(ListView pView, bool pIncludeHeader)
        {
            bool isFirst = true;
            StringBuilder sb = new StringBuilder(pView.Items.Count * 100);
            if (pIncludeHeader)
            {
                foreach (ColumnHeader ch in pView.Columns)
                {
                    if (!isFirst) sb.Append('\t');
                    sb.Append(ch.Text);
                    isFirst = false;
                }
            }
            sb.AppendLine();

            foreach (ListViewItem li in pView.Items)
            {
                for (int iSub = 0; iSub < li.SubItems.Count; iSub++)
                {
                    if (iSub > 0) sb.Append('\t');
                    sb.Append(li.SubItems[iSub].Text);
                }
                sb.AppendLine();
            }
            return sb.ToString();
        }

        /// <summary>Serialize content of ListViewItem into string</summary>
        /// <param name="pItem">ListViewItem to copy content from</param>
        /// <param name="pIncludeHeader">If need to include header (column names)</param>
        /// <returns>Content of ListView in form of string</returns>
        public static string LV_ToString(ListViewItem pItem, bool pIncludeHeader)
        {
            bool isFirst = true;
            StringBuilder sb = new StringBuilder(pItem.SubItems.Count * 50);
            if (pIncludeHeader)
            {
                foreach (ColumnHeader ch in pItem.ListView.Columns)
                {
                    if (!isFirst) sb.Append('\t');
                    sb.Append(ch.Text);
                    isFirst = false;
                }
            }
            sb.AppendLine();

            for (int iSub = 0; iSub < pItem.SubItems.Count; iSub++)
            {
                if (iSub > 0) sb.Append('\t');
                sb.Append(pItem.SubItems[iSub].Text);
            }
            sb.AppendLine();

            return sb.ToString();
        }

        /// <summary>Serialize content of ListViewGroup into string</summary>
        /// <param name="pGroup">ListViewGroup to copy content from</param>
        /// <param name="pIncludeHeader">If need to include header (column names)</param>
        /// <returns>Content of ListView in form of string</returns>
        public static string LV_ToString(ListViewGroup pGroup, bool pIncludeHeader)
        {
            bool isFirst = true;
            StringBuilder sb = new StringBuilder(pGroup.Items.Count * 100);
            if (pIncludeHeader)
            {
                foreach (ColumnHeader ch in pGroup.ListView.Columns)
                {
                    if (!isFirst) sb.Append('\t');
                    sb.Append(ch.Text);
                    isFirst = false;
                }
            }
            sb.AppendLine();

            foreach (ListViewItem li in pGroup.Items)
            {
                for (int iSub = 0; iSub < li.SubItems.Count; iSub++)
                {
                    if (iSub > 0) sb.Append('\t');
                    sb.Append(li.SubItems[iSub].Text);
                }
                sb.AppendLine();
            }

            return sb.ToString();
        }

        /// <summary>Get sibling of ListViewItem, the one which will be active after specified ListViewItem deleted</summary>
        /// <param name="pItem">The ListViewItem to search sibling for</param>
        /// <returns>Sibling of specified ListViewItem or null when no more items</returns>
        public static ListViewItem LV_GetSibling(ListViewItem pItem)
        {
            ListView pList = pItem.ListView;

            int idx = pItem.Index;
            if (idx > 0)
                return pList.Items[idx - 1];

            if ((idx + 1) < (pList.Items.Count - 1))
                return pList.Items[idx + 1];

            return null;
        }

        #endregion // LV: ListView

        #region MDI: Milti-Document Interface

        /// <summary>Deprecated. Please use MDI_FindChildByTag() method indead</summary>
        public static Form FindMdiChildByTag(Form pMdiForm, object pTag)
        {
            return MDI_FindChildByTag(pMdiForm, pTag);
        }

        /// <summary>Deprecated. Please use MDI_FindChildOfType() method indead</summary>
        public static Form FindMdiChildOf(Form pMdiContainer, Type pFormType)
        {
            return MDI_FindChildOfType(pMdiContainer, pFormType);
        }

        /// <summary>Find MDI child form by Tag</summary>
        /// <param name="pMdiForm">MDI parent form to search within childs of</param>
        /// <param name="pTag">Tag to find</param>
        /// <returns>Found MdiChild form or null when specified tag not found</returns>
        public static Form MDI_FindChildByTag(Form pMdiForm, object pTag)
        {
            foreach (Form frm in pMdiForm.MdiChildren)
            {
                if (frm.Tag.Equals(pTag))
                    return frm;
            }
            return null;
        }

        /// <summary>Find MDI child form by Type</summary>
        /// <param name="pMdiForm">MDI parent form to search within childs of</param>
        /// <param name="pFormType">Type MDI form to find</param>
        /// <returns>Found MdiChild form or null when specified not found</returns>
        public static Form MDI_FindChildOfType(Form pMdiContainer, Type pFormType)
        {
            foreach (Form child in pMdiContainer.MdiChildren)
            {
                if (child.GetType().Equals(pFormType))
                    return child;
            }
            return null;
        }

        #endregion // MDI: Milti-Document Interface

        #region MRU: Most-Recently-Used

        private delegate void PopulateMruMethod(System.Windows.Forms.ContextMenuStrip pMruItems, EventHandler pHandler, string pMruList);

        public static void PopulateMru(System.Windows.Forms.ContextMenuStrip pMruItems, EventHandler pHandler, string pMruList)
        {
            if (pMruItems.InvokeRequired)
            {
                pMruItems.BeginInvoke(new PopulateMruMethod(PopulateMru), pMruItems, pHandler, pMruList);
            }
            else
            {
                pMruItems.Items.Clear();
                string mruList = pMruList;
                if (!string.IsNullOrEmpty(mruList))
                {
                    string[] mruArr = mruList.Split('\n');
                    int idx = 1;
                    foreach (string mri in mruArr)
                    {
                        ToolStripItem mi = pMruItems.Items.Add(string.Format("&{0}. {1}", idx, mri));
                        mi.Click += pHandler;
                        mi.Tag = mri;
                        idx++;
                    }
                }
            }
        }

        private delegate void PopulateMruMethod2(System.Windows.Forms.ToolStripDropDownItem pMruItem, EventHandler pHandler, string pMruList);

        public static void PopulateMru(System.Windows.Forms.ToolStripDropDownItem pMruItem, EventHandler pHandler, string pMruList)
        {
            if (pMruItem.GetCurrentParent().InvokeRequired)
            {
                pMruItem.GetCurrentParent().BeginInvoke(new PopulateMruMethod2(PopulateMru), pMruItem, pHandler, pMruList);
            }
            else
            {
                pMruItem.DropDownItems.Clear();
                string mruList = pMruList;
                if (!string.IsNullOrEmpty(mruList))
                {
                    string[] mruArr = mruList.Split('\n');
                    int idx = 1;
                    foreach (string mri in mruArr)
                    {
                        ToolStripItem mi = pMruItem.DropDownItems.Add(string.Format("&{0}. {1}", idx, mri));
                        mi.Click += pHandler;
                        mi.Tag = mri;
                        idx++;
                    }
                }
            }
        }

        /// <summary>Add new item to MRU list, or when item already in list it will be moved to top position in list</summary>
        /// <param name="pFilename">Item to be added to MRU list</param>
        /// <param name="pMruList">MRU list serialized to string</param>
        /// <returns>New MRU list serialized to string</returns>
        public static string AddMruItem(string pFilename, string pMruList)
        {
            string mruList = pMruList;
            if (!string.IsNullOrEmpty(mruList))
            {
                mruList = mruList.Trim("\n".ToCharArray());
                mruList = mruList.Insert(0, pFilename + "\n");
                List<string> mruArr = new List<string>(mruList.Split('\n'));
                int n = mruArr.IndexOf(pFilename, 1);
                if (n >= 0)
                {
                    mruArr.RemoveAt(n);
                    mruList = packToMruStr(mruArr);
                }
                if (mruArr.Count > 9)
                {
                    while (mruArr.Count > 9)
                        mruArr.RemoveAt(mruArr.Count - 1);
                    mruList = packToMruStr(mruArr);
                }
            }
            else
                mruList = pFilename;

            pMruList = mruList;
            return pMruList;
        }

        /// <summary>Pack MRU list items into string, items delimited by '\n'</summary>
        private static string packToMruStr(List<string> pList)
        {
            string list = "";
            foreach (string item in pList)
                list += (item + "\n");
            list = list.Trim("\n".ToCharArray());
            return list;
        }

        #endregion // MRU: Most-Recently-Used

        #region TV: TreeView

        /// <summary>Build a path of node indexes for selected TreeNode</summary>
        /// <param name="pNode">TreeNode to build indexes path for</param>
        /// <param name="pTargetList">Target list to store indexes path. Note: list is not cleared before storing indexes path!</param>
        public static void TV_NodeIndexesPath(TreeNode pNode, List<int> pTargetList)
        {
            do
            {
                pTargetList.Add(pNode.Index);
                pNode = pNode.Parent;
            }
            while (pNode != null);
            pTargetList.Reverse();
        }

        /// <summary>Select TreeNode according to specified indexes path</summary>
        /// <param name="pTree">TreeView control to select TreeNode in</param>
        /// <param name="pPath">Indexes path</param>
        public static void TV_SelectByIndexesPath(TreeView pTree, List<int> pPath)
        {
            TreeNodeCollection nodesList = pTree.Nodes;
            TreeNode node, nodeToSelect = null;
            int iPath = 0;
            do
            {
                int iNode = pPath[iPath];
                node = (iNode < nodesList.Count ? nodesList[iNode] : null);
                if (node == null)
                    break;

                nodeToSelect = node;
                nodesList = nodeToSelect.Nodes;
                iPath++;
            }
            while (iPath < pPath.Count);

            if (nodeToSelect != null)
                pTree.SelectedNode = nodeToSelect;
        }

        /// <summary>Synchronize TreeNode selection between 2 TreeViews</summary>
        /// <param name="pSource">Source TreeView to sync from</param>
        /// <param name="pTarget">Target TreeView to sync to</param>
        public static void TV_SyncSelection(TreeView pSource, TreeView pTarget)
        {
            if (pSource.SelectedNode == null)
            {
                pTarget.SelectedNode = null;
                return;
            }
            List<int> indexes = new List<int>();
            TV_NodeIndexesPath(pSource.SelectedNode, indexes);
            TV_SelectByIndexesPath(pTarget, indexes);
        }

        /// <summary>Find TreeNode matching specified Tag object</summary>
        /// <param name="pNode">TreeNode to search matching Tag object in</param>
        /// <param name="pTagToFind">Tag object to search</param>
        /// <param name="pRecursive">If it should search recusively to max available tree depth</param>
        /// <returns>TreeNode with matching Tag object of null when not found</returns>
        public static TreeNode TV_FindByTag(TreeNode pNode, object pTagToFind, bool pRecursive)
        {
            if (object.Equals(pTagToFind, pNode.Tag))
                return pNode;

            foreach (TreeNode tn in pNode.Nodes)
            {
                if (object.Equals(pTagToFind, tn.Tag))
                    return tn;
            }

            if (pRecursive)
            {
                foreach (TreeNode tn in pNode.Nodes)
                {
                    TreeNode t = TV_FindByTag(tn, pTagToFind, pRecursive);
                    if (t != null)
                        return t;
                }
            }
            return null;
        }

        /// <summary>Delegate method to use by ForEachTreeNode method</summary>
        /// <param name="pNode"></param>
        /// <returns>It should return true to continue, it should return false to stop on this node</returns>
        public delegate bool TreeNodeHandler(TreeNode pNode);

        /// <summary>Iterate over all notes in TreeView</summary>
        /// <param name="pNodes">Collection of tree nodes to iterate over</param>
        /// <param name="pHandler">Delegate to call for every tree node</param>
        /// <returns>Return true if it was go through all tree nodes, false when it was stop on some node</returns>
        public static bool ForEachTreeNode(TreeNodeCollection pNodes, TreeNodeHandler pHandler)
        {
            foreach (TreeNode tn in pNodes)
            {
                if (!pHandler(tn))
                    return false;
                if (tn.Nodes.Count > 0)
                {
                    if (!ForEachTreeNode(tn.Nodes, pHandler))
                        return false;
                }
            }
            return true;
        }

        /// <summary>Delegate method to use by ForEachTreeNodeEx method</summary>
        /// <param name="pNode"></param>
        /// <returns>It should return true to continue, it should return false to stop on this node</returns>
        public delegate bool TreeNodeHandlerEx(TreeNode pNode, object pContext);

        /// <summary>Iterate over all notes in TreeView</summary>
        /// <param name="pNodes">Collection of tree nodes to iterate over</param>
        /// <param name="pHandler">Delegate to call for every tree node</param>
        /// <param name="pContext">Context object to pass as parameter</param>
        /// <returns>Return true if it was go through all tree nodes, false when it was stop on some node</returns>
        public static TreeNode ForEachTreeNodeEx(TreeNodeCollection pNodes, TreeNodeHandlerEx pHandler, object pContext)
        {
            foreach (TreeNode tn in pNodes)
            {
                if (!pHandler(tn, pContext))
                    return tn;
                if (tn.Nodes.Count > 0)
                {
                    TreeNode t = ForEachTreeNodeEx(tn.Nodes, pHandler, pContext);
                    if (t != null)
                        return t;
                }
            }
            return null;
        }

        #endregion // TV: TreeView

        #region TB: ToolBar

        /// <summary>Clear checkmark states for menu items in specified ToolStripDropDownButton</summary>
        public static void TB_ClearCheckmarks(ToolStripDropDownButton tb)
        {
            foreach (ToolStripItem it in tb.DropDownItems)
            {
                if (!(it is ToolStripMenuItem)) continue;
                ToolStripMenuItem mi = (ToolStripMenuItem)it;
                mi.Checked = false;
            }
        }

        #endregion // TB: ToolBar

        #region Implementation details

        private static void handleColorAttribute(System.Windows.Forms.RichTextBox pTarget, XmlElement node, ref Color savedColor)
        {
            XmlNode attr = node.Attributes.GetNamedItem("color");
            if (attr == null)
                attr = node.Attributes.GetNamedItem("value");
            if (attr != null)
            {
                savedColor = pTarget.SelectionColor;
                string id = attr.Value;
                if (id.StartsWith("#"))
                    pTarget.SelectionColor = Color.FromArgb(Convert.ToInt32(id.Remove(0, 1), 16));
                else
                    pTarget.SelectionColor = Color.FromName(id);
            }
        }

        private static FontStyle[] FontStyle_Values = new FontStyle[] { 
            FontStyle.Regular, FontStyle.Bold, FontStyle.Italic, FontStyle.Underline, FontStyle.Strikeout 
        };

        private static void handleFontAttributes(System.Windows.Forms.RichTextBox pTarget, XmlElement node, ref Font savedFont)
        {
            savedFont = pTarget.SelectionFont;

            string name = pTarget.SelectionFont.Name;
            float size = pTarget.SelectionFont.Size;
            FontStyle style = pTarget.SelectionFont.Style;
            
            string s;
            XmlNode attr = node.Attributes.GetNamedItem("name");
            if (attr != null) name = attr.Value;
            attr = node.Attributes.GetNamedItem("size");
            if (attr != null)
            {
                s = attr.Value;
                int delta = 0;
                bool isDelta = s.StartsWith("+") || s.StartsWith("-");
                if (isDelta)
                {
                    delta = (s.StartsWith("+") ? 1 : -1);
                    s = s.Remove(0, 1);                    
                }
                float sz;
                float.TryParse(s, out sz);
                if (delta != 0)
                    size += (sz * delta);
                else
                    size = sz;
            }
            attr = node.Attributes.GetNamedItem("style");
            if (attr != null)
            {
                string[] items = attr.Value.Replace(';', ',').Split(',');
                foreach (string it in items)
                {
                    s = it;
                    bool isNegation = s.StartsWith("-");
                    if (isNegation) s = s.Remove(0, 1);
                    int n;
                    if (!StrUtils.GetAsEnum(s, "r,b,i,u,s,Regular,Bold,Italic,Underline,Strikeout", out n)) continue;
                    n %= FontStyle_Values.Length;
                    FontStyle st = FontStyle_Values[n];
                    if (isNegation)
                        style &= ~st;
                    else
                        style |= st;
                }
            }            

            Font fnt = new Font(name, size, style);
            pTarget.SelectionFont = fnt;
        }

        private static void renderDomNodeInto(System.Windows.Forms.RichTextBox pTarget, XmlElement pDomNode)
        {
            foreach (XmlNode node in pDomNode.ChildNodes)
            {
                if (node.NodeType == XmlNodeType.CDATA || node.NodeType == XmlNodeType.Text)
                {
                    pTarget.AppendText(node.Value);
                }
                else if (node.NodeType == XmlNodeType.Element)
                {
                    string toAddAfter = null;
                    Color savedColor = Color.Empty;
                    Font savedFont = null;
                    switch (node.Name.ToLower())
                    {
                        case "br":
                            pTarget.AppendText(Environment.NewLine);
                            break;
                        case "p":
                            handleColorAttribute(pTarget, (XmlElement)node, ref savedColor);
                            pTarget.AppendText(Environment.NewLine);
                            toAddAfter = Environment.NewLine;
                            break;
                        case "b":
                            {
                                handleColorAttribute(pTarget, (XmlElement)node, ref savedColor);
                                Font fnt = new Font(pTarget.SelectionFont.Name, pTarget.SelectionFont.Size, pTarget.SelectionFont.Style | FontStyle.Bold);
                                savedFont = pTarget.SelectionFont;
                                pTarget.SelectionFont = fnt;
                            }
                            break;
                        case "i":
                            {
                                handleColorAttribute(pTarget, (XmlElement)node, ref savedColor);
                                Font fnt = new Font(pTarget.SelectionFont.Name, pTarget.SelectionFont.Size, pTarget.SelectionFont.Style | FontStyle.Italic);
                                savedFont = pTarget.SelectionFont;
                                pTarget.SelectionFont = fnt;
                            }
                            break;
                        case "u":
                            {
                                handleColorAttribute(pTarget, (XmlElement)node, ref savedColor);
                                Font fnt = new Font(pTarget.SelectionFont.Name, pTarget.SelectionFont.Size, pTarget.SelectionFont.Style | FontStyle.Underline);
                                savedFont = pTarget.SelectionFont;
                                pTarget.SelectionFont = fnt;
                            }
                            break;
                        case "color":
                            handleColorAttribute(pTarget, (XmlElement)node, ref savedColor);
                            break;
                        case "font":
                            handleColorAttribute(pTarget, (XmlElement)node, ref savedColor);
                            handleFontAttributes(pTarget, (XmlElement)node, ref savedFont);
                            break;
                    }
                    renderDomNodeInto(pTarget, (XmlElement)node);
                    if (savedFont != null)
                        pTarget.SelectionFont = savedFont;
                    if (savedColor != Color.Empty)
                        pTarget.SelectionColor = savedColor;
                    if (toAddAfter != null)
                        pTarget.AppendText(toAddAfter);
                }
            }
        }

        #endregion // Implementation details
    }
}
