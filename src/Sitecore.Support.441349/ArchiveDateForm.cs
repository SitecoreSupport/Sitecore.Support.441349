using Sitecore;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.Globalization;
using Sitecore.Security.AccessControl;
using Sitecore.Web;
using Sitecore.Web.UI.HtmlControls;
using Sitecore.Web.UI.Pages;
using Sitecore.Web.UI.Sheer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace Sitecore.Support.Shell.Applications.Dialogs.ArchiveDate
{
    /// <summary>
    /// Represents a ArchiveDateForm.
    /// </summary>
    public class ArchiveDateForm : DialogForm
    {
        /// <summary>
        ///   The item archive date.
        /// </summary>
        protected DateTimePicker ItemArchiveDate;

        /// <summary>
        ///   The versions.
        /// </summary>
        protected Border Versions;

        /// <summary>
        ///   Gets the current item.
        /// </summary>
        protected Item CurrentItem
        {
            get
            {
                ID d;
                Assert.IsTrue(ID.TryParse(WebUtil.GetQueryString("id"), out d), "item id");
                Database database = Database.GetDatabase(WebUtil.GetQueryString("db"));
                Assert.IsNotNull(database, "database");
                Item item = database.GetItem(d);
                Assert.IsNotNull(item, "Item not found");
                return item;
            }
        }

        public ArchiveDateForm()
        {
        }

        /// <summary>
        /// Raises the load event.
        /// </summary>
        /// <param name="e">
        /// The <see cref="T:System.EventArgs" /> instance containing the event data.
        /// </param>
        /// <remarks>
        /// This method notifies the server control that it should perform actions common to each HTTP
        ///   request for the page it is associated with, such as setting up a database query. At this
        ///   stage in the page lifecycle, server controls in the hierarchy are created and initialized,
        ///   view state is restored, and form controls reflect client-side data. Use the IsPostBack
        ///   property to determine whether the page is being loaded in response to a client postback,
        ///   or if it is being loaded and accessed for the first time.
        /// </remarks>
        protected override void OnLoad(EventArgs e)
        {
            Assert.ArgumentNotNull(e, "e");
            Item item = Client.CoreDatabase.GetItem("/sitecore/content/Applications/Content Editor/Ribbons/Chunks/Schedule/Archive");
            Assert.HasAccess((item == null ? false : item.Access.CanRead()), "Access denied");
            base.OnLoad(e);
            if (!Context.ClientPage.IsEvent)
            {
                if (!string.IsNullOrEmpty(this.CurrentItem[FieldIDs.ArchiveDate]))
                {
                    this.ItemArchiveDate.Value = DateUtil.IsoDateToServerTimeIsoDate(this.CurrentItem[FieldIDs.ArchiveDate]);
                }
                this.RenderVersions(this.CurrentItem);
            }
        }

        /// <summary>
        /// The ok click handler.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="args">The args.</param>
        protected override void OnOK(object sender, EventArgs args)
        {
            Assert.ArgumentNotNull(sender, "sender");
            Assert.ArgumentNotNull(args, "args");
            if (!this.SetItemArchiveDate(this.CurrentItem, this.ItemArchiveDate.Value))
            {
                return;
            }
            if (!this.SetVersionsArchiveDate(this.CurrentItem.Versions.GetVersions(true)))
            {
                return;
            }
            base.OnOK(sender, args);
        }

        /// <summary>
        /// Renders the versions.
        /// </summary>
        /// <param name="item">
        /// The item.
        /// </param>
        protected void RenderVersions(Item item)
        {
            Assert.ArgumentNotNull(item, "item");
            IOrderedEnumerable<Item> versions =
                from i in (IEnumerable<Item>)item.Versions.GetVersions(true)
                orderby i.Language.Name, i.Version.Number
                select i;
            StringBuilder stringBuilder = new StringBuilder("<table width=\"100%\" class=\"scListControl\" cellpadding=\"0\" cellspacing=\"0\">");
            stringBuilder.Append("<tr>");
            stringBuilder.Append(string.Concat("<td><b>", Translate.Text("Language"), "</b></td>"));
            stringBuilder.Append(string.Concat("<td><b>", Translate.Text("Version"), "</b></td>"));
            stringBuilder.Append(string.Concat("<td width=\"100%\"><b>", Translate.Text("Date and time"), "</b></td>"));
            stringBuilder.Append("</tr>");
            this.Versions.Controls.Add(new LiteralControl(stringBuilder.ToString()));
            foreach (Item version in versions)
            {
                stringBuilder = new StringBuilder();
                stringBuilder.AppendFormat("<tr><td style=\"text-align:center\"><b>{0}</b><td style=\"text-align:center\"><b>{1}</b></td>", version.Language.Name, version.Version.Number);
                stringBuilder.Append("<td>");
                this.Versions.Controls.Add(new LiteralControl(stringBuilder.ToString()));
                DateTimePicker dateTimePicker = new DateTimePicker()
                {
                    ID = string.Concat("archive_", version.Language.Name, version.Version.Number),
                    Width = new Unit(100, UnitType.Percentage)
                };
                if (!string.IsNullOrEmpty(version[FieldIDs.ArchiveVersionDate]))
                {
                    dateTimePicker.Value = DateUtil.IsoDateToServerTimeIsoDate(version[FieldIDs.ArchiveVersionDate]);
                }
                this.Versions.Controls.Add(dateTimePicker);
                this.Versions.Controls.Add(new LiteralControl("</td></tr>"));
            }
            this.Versions.Controls.Add(new LiteralControl("</table>"));
        }

        /// <summary>
        /// Set archive date for the whole item
        /// </summary>
        /// <param name="item">
        /// The item.
        /// </param>
        /// <param name="value">
        /// The value.
        /// </param>
        /// <returns>
        /// The set item archive date.
        /// </returns>
        protected virtual bool SetItemArchiveDate(Item item, string value)
        {
            Assert.ArgumentNotNull(item, "item");
            Assert.ArgumentNotNull(value, "value");
            DateTime universalTime = DateUtil.ToUniversalTime(DateUtil.IsoDateToDateTime(value, DateTime.MinValue));
            if (!string.IsNullOrEmpty(value) && universalTime == DateTime.MinValue)
            {
                SheerResponse.Alert(Translate.Text("Failed to parse date {0}. No Changes has been made.", new object[] { value }), Array.Empty<string>());
                return false;
            }
            if (!string.IsNullOrEmpty(value))
            {
                Log.Audit(this, "Set Item Archive date: {0}, date: {1}", new string[] { AuditFormatter.FormatItem(item), DateUtil.DateTimeToMilitary(universalTime) });
            }
            else if (!string.IsNullOrEmpty(item[FieldIDs.ArchiveDate]))
            {
                Log.Audit(this, "Clear Item Archive date: {0}", new string[] { AuditFormatter.FormatItem(item) });
            }
            using (EditContext editContext = new EditContext(item))
            {
                item[FieldIDs.ArchiveDate] = (string.IsNullOrEmpty(value) ? string.Empty : DateUtil.ToIsoDate(universalTime));
            }
            return true;
        }

        /// <summary>
        /// Sets the versions archive date.
        /// </summary>
        /// <param name="versions">
        /// The versions.
        /// </param>
        /// <returns>
        /// The set versions archive date.
        /// </returns>
        protected bool SetVersionsArchiveDate(IEnumerable<Item> versions)
        {
            Assert.ArgumentNotNull(versions, "versions");
            foreach (Item version in versions)
            {
                DateTimePicker dateTimePicker = this.Versions.FindControl(string.Concat("archive_", version.Language.Name, version.Version.Number)) as DateTimePicker;
                if (dateTimePicker == null)
                {
                    continue;
                }
                DateTime universalTime = DateUtil.ToUniversalTime(DateUtil.IsoDateToDateTime(dateTimePicker.Value, DateTime.MinValue));
                if (universalTime != DateTime.MinValue)
                {
                    Log.Audit(this, "Set Version Archive date: {0}, date: {1}", new string[] { AuditFormatter.FormatItem(version), DateUtil.DateTimeToMilitary(universalTime) });
                }
                else if (!string.IsNullOrEmpty(version[FieldIDs.ArchiveVersionDate]))
                {
                    Log.Audit(this, "Clear Version Archive date: {0}", new string[] { AuditFormatter.FormatItem(version) });
                }
                using (EditContext editContext = new EditContext(version))
                {
                    version[FieldIDs.ArchiveVersionDate] = (universalTime == DateTime.MinValue ? string.Empty : DateUtil.ToIsoDate(universalTime));
                }
            }
            return true;
        }
    }
}