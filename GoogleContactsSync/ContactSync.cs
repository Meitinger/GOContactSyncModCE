using System;
using System.Collections.Generic;
using System.Text;
using Google.GData.Client;
using Google.GData.Contacts;
using Google.GData.Extensions;
using Outlook = Microsoft.Office.Interop.Outlook;
using Google.Contacts;

namespace GoContactSyncMod
{
    
	internal static class ContactSync
	{
        private static DateTime outlookDateNone = new DateTime(4501, 1, 1);
        private const string relSpouse = "spouse";
        private const string relChild = "child";
        private const string relAnniversary = "anniversary";
        private const string relHomePage = "home-page";
    
        //public static void UpdateContact(Contact source, Outlook.ContactItem destination)
        //{
        //    //// if no email or number, contact will be updated at each sync
        //    //if (source.Emails.Count == 0 && source.Phonenumbers.Count == 0)
        //    //    return;


        //    if (!string.IsNullOrEmpty(source.Title))
        //        destination.FileAs = source.Title;
        //    else
        //        destination.FileAs = source.Emails[0].Address;

        //    SetEmails(source, destination);

        //    //First delete the destination phone numbers having secondary phone numbers
        //    destination.HomeTelephoneNumber = null;     //secondary: destination.Home2TelephoneNumber
        //    destination.BusinessTelephoneNumber = null; //secondary: destination.Business2TelephoneNumber

        //    foreach (PhoneNumber phone in source.Phonenumbers)
        //    {
        //        SetPhoneNumber(phone, destination);
        //    }

        //    foreach (PostalAddress address in source.PostalAddresses)
        //    {
        //        SetPostalAddress(address, destination);
        //    }

        //    destination.Companies = string.Empty;
        //    foreach (Organization company in source.Organizations)
        //    {
        //        if (company.Primary)
        //        {
        //            destination.CompanyName = company.Name;
        //            destination.JobTitle = company.Title;
        //        }
        //        if (destination.Companies.Length > 0)
        //            destination.Companies += "; ";
        //        destination.Companies += company.Name;
        //    }

        //    destination.IMAddress = "";
        //    foreach (IMAddress im in source.IMs)
        //    {
        //        if (destination.IMAddress.Length > 0)
        //            destination.IMAddress += "; ";
        //        if (!string.IsNullOrEmpty(im.Protocol))
        //            destination.IMAddress += im.Protocol + ": " + im.Address;
        //        destination.IMAddress += im.Address;
        //    }

        //    destination.Body = source.Content.Content;
        //}

        ///// <summary>
        ///// Replaces all properties of <paramref name="destination"/> from corresponding properties of <paramref name="source"/>
        ///// </summary>
        ///// <param name="source"></param>
        ///// <param name="destination"></param>
        //public static void UpdateContact(Outlook.ContactItem source, Contact destination)
        //{
        //    //// if no email or number, contact will be updated at each sync
        //    //if (string.IsNullOrEmpty(source.Email1Address) && string.IsNullOrEmpty(source.PrimaryTelephoneNumber))
        //    //    return;

        //    if (source.FileAs != source.Email1Address)
        //        destination.Title = source.FileAs;
        //    else
        //        destination.Title = null;

        //    if (destination.Title == null)
        //        destination.Title = source.FullName;

        //    if (destination.Title == null)
        //        destination.Title = source.CompanyName;

        //    SetEmails(source, destination);

        //    SetPhoneNumbers(source, destination);

        //    SetAddresses(source, destination);

        //    SetCompanies(source, destination);

        //    SetIMs(source, destination);

        //    // CH - Fixed error with invalid xml being sent to google... This may need to be added to everything
        //    destination.Content.Content = String.Format("<![CDATA[{0}]]>", source.Body);
        //}

		public static void SetAddresses(Outlook.ContactItem source, Contact destination)
		{
            destination.PostalAddresses.Clear();

			if (!string.IsNullOrEmpty(source.HomeAddress))
			{
				StructuredPostalAddress postalAddress = new StructuredPostalAddress();
                postalAddress.Street = source.HomeAddressStreet;
                postalAddress.City = source.HomeAddressCity;
                postalAddress.Postcode = source.HomeAddressPostalCode;
                postalAddress.Country = source.HomeAddressCountry;
                postalAddress.Primary = destination.PostalAddresses.Count == 0;
				postalAddress.Rel = ContactsRelationships.IsHome;
				destination.PostalAddresses.Add(postalAddress);
			}

			if (!string.IsNullOrEmpty(source.BusinessAddress))
			{
				StructuredPostalAddress postalAddress = new StructuredPostalAddress();
                postalAddress.Street = source.BusinessAddressStreet;
                postalAddress.City = source.BusinessAddressCity;
                postalAddress.Postcode = source.BusinessAddressPostalCode;
                postalAddress.Country = source.BusinessAddressCountry;
				postalAddress.Primary = destination.PostalAddresses.Count == 0;
				postalAddress.Rel = ContactsRelationships.IsWork;
				destination.PostalAddresses.Add(postalAddress);
			}

			if (!string.IsNullOrEmpty(source.OtherAddress))
			{
				StructuredPostalAddress postalAddress = new StructuredPostalAddress();
                postalAddress.Street = source.OtherAddressStreet;
                postalAddress.City = source.OtherAddressCity;
                postalAddress.Postcode = source.OtherAddressPostalCode;
                postalAddress.Country = source.OtherAddressCountry;
				postalAddress.Primary = destination.PostalAddresses.Count == 0;
				postalAddress.Rel = ContactsRelationships.IsOther;
				destination.PostalAddresses.Add(postalAddress);
			}
		}

		public static void SetIMs(Outlook.ContactItem source, Contact destination)
		{
            destination.IMs.Clear();

			if (!string.IsNullOrEmpty(source.IMAddress))
			{
				//IMAddress are expected to be in form of ([Protocol]: [Address]; [Protocol]: [Address])
				string[] imsRaw = source.IMAddress.Split(';');
				foreach (string imRaw in imsRaw)
				{
					string[] imDetails = imRaw.Trim().Split(':');
					IMAddress im = new IMAddress();
					if (imDetails.Length == 1)
						im.Address = imDetails[0].Trim();
					else
					{
						im.Protocol = imDetails[0].Trim();
						im.Address = imDetails[1].Trim();
					}
					im.Primary = destination.IMs.Count == 0;
					im.Rel = ContactsRelationships.IsHome;
					destination.IMs.Add(im);
				}
			}
		}

		public static void SetEmails(Outlook.ContactItem source, Contact destination)
		{
            destination.Emails.Clear();

			if (!string.IsNullOrEmpty(source.Email1Address))
			{
				EMail primaryEmail = new EMail(source.Email1Address);
				primaryEmail.Primary = destination.Emails.Count == 0;
				primaryEmail.Rel = ContactsRelationships.IsWork;
				destination.Emails.Add(primaryEmail);
			}

			if (!string.IsNullOrEmpty(source.Email2Address))
			{
				EMail secondaryEmail = new EMail(source.Email2Address);
				secondaryEmail.Primary = destination.Emails.Count == 0;
				secondaryEmail.Rel = ContactsRelationships.IsHome;
				destination.Emails.Add(secondaryEmail);
			}

			if (!string.IsNullOrEmpty(source.Email3Address))
			{
				EMail secondaryEmail = new EMail(source.Email3Address);
				secondaryEmail.Primary = destination.Emails.Count == 0;
				secondaryEmail.Rel = ContactsRelationships.IsOther;
				destination.Emails.Add(secondaryEmail);
			}
		}

		public static void SetPhoneNumbers(Outlook.ContactItem source, Contact destination)
		{

            destination.Phonenumbers.Clear();

            if (!string.IsNullOrEmpty(source.PrimaryTelephoneNumber))
            {
                //ToDo: Temporary cleanup algorithm to get rid of duplicate primary phone numbers
                //Can be removed once the contacts are clean for all users:
                if (source.PrimaryTelephoneNumber.Equals(source.MobileTelephoneNumber))
                {
                    //Reset primary TelephoneNumber because it is duplicate, and maybe even MobilePhone Number if duplicate
                    source.PrimaryTelephoneNumber = string.Empty;
                    if (source.MobileTelephoneNumber.Equals(source.HomeTelephoneNumber) ||
                        source.MobileTelephoneNumber.Equals(source.Home2TelephoneNumber) ||
                        source.MobileTelephoneNumber.Equals(source.BusinessTelephoneNumber) ||
                        source.MobileTelephoneNumber.Equals(source.Business2TelephoneNumber) ||
                        source.MobileTelephoneNumber.Equals(source.HomeFaxNumber) ||
                        source.MobileTelephoneNumber.Equals(source.BusinessFaxNumber) ||
                        source.MobileTelephoneNumber.Equals(source.OtherTelephoneNumber) ||
                        source.MobileTelephoneNumber.Equals(source.PagerNumber) ||
                        source.MobileTelephoneNumber.Equals(source.CarTelephoneNumber))
                    {
                        source.MobileTelephoneNumber = string.Empty;
                    }

                }
                else if (source.PrimaryTelephoneNumber.Equals(source.HomeTelephoneNumber) ||
                        source.PrimaryTelephoneNumber.Equals(source.Home2TelephoneNumber) ||
                        source.PrimaryTelephoneNumber.Equals(source.BusinessTelephoneNumber) ||
                        source.PrimaryTelephoneNumber.Equals(source.Business2TelephoneNumber) ||
                        source.PrimaryTelephoneNumber.Equals(source.HomeFaxNumber) ||
                        source.PrimaryTelephoneNumber.Equals(source.BusinessFaxNumber) ||
                        source.PrimaryTelephoneNumber.Equals(source.OtherTelephoneNumber) ||
                        source.PrimaryTelephoneNumber.Equals(source.PagerNumber) ||
                        source.PrimaryTelephoneNumber.Equals(source.CarTelephoneNumber))
                {
                    //Reset primary TelephoneNumber because it is duplicate
                    source.PrimaryTelephoneNumber = string.Empty;
                }
                else
                {
                    PhoneNumber phoneNumber = new PhoneNumber(source.PrimaryTelephoneNumber);
                    phoneNumber.Primary = destination.Phonenumbers.Count == 0;
                    phoneNumber.Rel = ContactsRelationships.IsMain;
                    destination.Phonenumbers.Add(phoneNumber);
                }
            }

			if (!string.IsNullOrEmpty(source.MobileTelephoneNumber))
			{
				PhoneNumber phoneNumber = new PhoneNumber(source.MobileTelephoneNumber);
                phoneNumber.Primary = destination.Phonenumbers.Count == 0;
				phoneNumber.Rel = ContactsRelationships.IsMobile;
				destination.Phonenumbers.Add(phoneNumber);
			}

			if (!string.IsNullOrEmpty(source.HomeTelephoneNumber))
			{
				PhoneNumber phoneNumber = new PhoneNumber(source.HomeTelephoneNumber);
                phoneNumber.Primary = destination.Phonenumbers.Count == 0;
				phoneNumber.Rel = ContactsRelationships.IsHome;
				destination.Phonenumbers.Add(phoneNumber);
			}

            if (!string.IsNullOrEmpty(source.Home2TelephoneNumber))
            {
                PhoneNumber phoneNumber = new PhoneNumber(source.Home2TelephoneNumber);
                phoneNumber.Primary = destination.Phonenumbers.Count == 0;
                phoneNumber.Rel = ContactsRelationships.IsHome;
                destination.Phonenumbers.Add(phoneNumber);
            }

			if (!string.IsNullOrEmpty(source.BusinessTelephoneNumber))
			{
				PhoneNumber phoneNumber = new PhoneNumber(source.BusinessTelephoneNumber);
                phoneNumber.Primary = destination.Phonenumbers.Count == 0;
				phoneNumber.Rel = ContactsRelationships.IsWork;
				destination.Phonenumbers.Add(phoneNumber);
			}

			if (!string.IsNullOrEmpty(source.Business2TelephoneNumber))
			{
				PhoneNumber phoneNumber = new PhoneNumber(source.Business2TelephoneNumber);
				phoneNumber.Primary = destination.Phonenumbers.Count == 0;
				phoneNumber.Rel = ContactsRelationships.IsWork;
				destination.Phonenumbers.Add(phoneNumber);
			}

			if (!string.IsNullOrEmpty(source.HomeFaxNumber))
			{
				PhoneNumber phoneNumber = new PhoneNumber(source.HomeFaxNumber);
				phoneNumber.Primary = destination.Phonenumbers.Count == 0;
				phoneNumber.Rel = ContactsRelationships.IsHomeFax;
				destination.Phonenumbers.Add(phoneNumber);
			}

			if (!string.IsNullOrEmpty(source.BusinessFaxNumber))
			{
				PhoneNumber phoneNumber = new PhoneNumber(source.BusinessFaxNumber);
				phoneNumber.Primary = destination.Phonenumbers.Count == 0;
				phoneNumber.Rel = ContactsRelationships.IsWorkFax;
				destination.Phonenumbers.Add(phoneNumber);
			}

			if (!string.IsNullOrEmpty(source.OtherTelephoneNumber))
			{
				PhoneNumber phoneNumber = new PhoneNumber(source.OtherTelephoneNumber);
				phoneNumber.Primary = destination.Phonenumbers.Count == 0;
				phoneNumber.Rel = ContactsRelationships.IsOther;
				destination.Phonenumbers.Add(phoneNumber);
			}

            //ToDo: Currently IsSatellite is returned as invalid Rel value
            //if (!string.IsNullOrEmpty(source.RadioTelephoneNumber))
            //{
            //    PhoneNumber phoneNumber = new PhoneNumber(source.RadioTelephoneNumber);
            //    phoneNumber.Primary = destination.Phonenumbers.Count == 0;
            //    phoneNumber.Rel = ContactsRelationships.IsSatellite;
            //    destination.Phonenumbers.Add(phoneNumber);
            //}

			if (!string.IsNullOrEmpty(source.PagerNumber))
			{
				PhoneNumber phoneNumber = new PhoneNumber(source.PagerNumber);
				phoneNumber.Primary = destination.Phonenumbers.Count == 0;
				phoneNumber.Rel = ContactsRelationships.IsPager;
				destination.Phonenumbers.Add(phoneNumber);
			}

			if (!string.IsNullOrEmpty(source.CarTelephoneNumber))
			{
				PhoneNumber phoneNumber = new PhoneNumber(source.CarTelephoneNumber);
				phoneNumber.Primary = destination.Phonenumbers.Count == 0;
				phoneNumber.Rel = ContactsRelationships.IsCar;
				destination.Phonenumbers.Add(phoneNumber);
			}            

		}

		public static void SetCompanies(Outlook.ContactItem source, Contact destination)
		{
            destination.Organizations.Clear();

			if (!string.IsNullOrEmpty(source.Companies))
			{
				//Companies are expected to be in form of "[Company]; [Company]".
				string[] companiesRaw = source.Companies.Split(';');
				foreach (string companyRaw in companiesRaw)
				{
					Organization company = new Organization();
                    company.Name = (destination.Organizations.Count == 0) ? source.CompanyName : null;
                    company.Title = (destination.Organizations.Count == 0)?source.JobTitle : null;
                    company.Department = (destination.Organizations.Count == 0) ? source.Department : null;
					company.Primary = destination.Organizations.Count == 0;
					company.Rel = ContactsRelationships.IsWork;
					destination.Organizations.Add(company);
				}
			}

			if (destination.Organizations.Count == 0 && (!string.IsNullOrEmpty(source.CompanyName) || !string.IsNullOrEmpty(source.JobTitle) || !string.IsNullOrEmpty(source.Department)))
			{
				Organization company = new Organization();
				company.Name = source.CompanyName;
                company.Title = source.JobTitle;
                company.Department = source.Department;
				company.Primary = true;
				company.Rel = ContactsRelationships.IsWork;
				destination.Organizations.Add(company);
			}
		}

		public static void SetPhoneNumber(PhoneNumber phone, Outlook.ContactItem destination)
		{
            //if (phone.Primary)
            if (phone.Rel == ContactsRelationships.IsMain)
                destination.PrimaryTelephoneNumber = phone.Value;
            else if (phone.Rel == ContactsRelationships.IsHome)
            {
                if (destination.HomeTelephoneNumber == null)
                    destination.HomeTelephoneNumber = phone.Value;
                else
                    destination.Home2TelephoneNumber = phone.Value;
            }
            else if (phone.Rel == ContactsRelationships.IsWork)
            {
                if (destination.BusinessTelephoneNumber == null)
                    destination.BusinessTelephoneNumber = phone.Value;
                else
                    destination.Business2TelephoneNumber = phone.Value;
            }
            else if (phone.Rel == ContactsRelationships.IsMobile)
			{
				destination.MobileTelephoneNumber = phone.Value;
				//destination.PrimaryTelephoneNumber = phone.Value;
			}
			else if (phone.Rel == ContactsRelationships.IsWorkFax)
				destination.BusinessFaxNumber = phone.Value;
			else if (phone.Rel == ContactsRelationships.IsHomeFax)
				destination.HomeFaxNumber = phone.Value;
            else if (phone.Rel == ContactsRelationships.IsPager)
				destination.PagerNumber = phone.Value;
            //else if (phone.Rel == ContactsRelationships.IsSatellite)
            //    destination.RadioTelephoneNumber = phone.Value;
			else if (phone.Rel == ContactsRelationships.IsOther)
				destination.OtherTelephoneNumber = phone.Value;
			else if (phone.Rel == ContactsRelationships.IsCar)
				destination.CarTelephoneNumber = phone.Value;
            //else if (phone.Rel == ContactsRelationships.IsVoip)
            //    destination.Business2TelephoneNumber = phone.Value;
            //else no phone category matches                       

		}

		public static void SetPostalAddress(StructuredPostalAddress address, Outlook.ContactItem destination)
		{
			if (address.Rel == ContactsRelationships.IsHome)
			{
                destination.HomeAddressStreet=address.Street;
                destination.HomeAddressCity=address.City;
                destination.HomeAddressPostalCode=address.Postcode;
                destination.HomeAddressCountry=address.Country;

				if (address.Primary)
					destination.SelectedMailingAddress = Microsoft.Office.Interop.Outlook.OlMailingAddress.olHome;
				return;
			}
			if (address.Rel == ContactsRelationships.IsWork)
			{
                destination.BusinessAddressStreet = address.Street;
                destination.BusinessAddressCity = address.City;
                destination.BusinessAddressPostalCode = address.Postcode;
                destination.BusinessAddressCountry = address.Country;

				if (address.Primary)
					destination.SelectedMailingAddress = Microsoft.Office.Interop.Outlook.OlMailingAddress.olBusiness;
				return;
			}
			if (address.Rel == ContactsRelationships.IsOther)
			{
                destination.OtherAddressStreet = address.Street;
                destination.OtherAddressCity = address.City;
                destination.OtherAddressPostalCode = address.Postcode;
                destination.OtherAddressCountry = address.Country;

				if (address.Primary)
					destination.SelectedMailingAddress = Microsoft.Office.Interop.Outlook.OlMailingAddress.olOther;
				return;
			}
		}

	public static void MergeContacts(Outlook.ContactItem master, Contact slave)
		{
			//// if no email or number, contact will be updated at each sync
            //if (string.IsNullOrEmpty(master.Email1Address) && string.IsNullOrEmpty(master.PrimaryTelephoneNumber))
            //{
            //    if (slave.Emails.Count > 0)
            //    {
            //        Logger.Log("Outlook Contact '" + master.FullNameAndCompany + "' has neither E-Mail address nor phone number. Setting E-Mail address of Google contact: " + slave.Emails[0].Address, EventType.Warning);
            //        master.Email1Address = slave.Emails[0].Address;
            //    }
            //    else
            //    {
            //        Logger.Log("Outlook Contact '" + master.FullNameAndCompany + "' has neither E-Mail address nor phone number. Cannot merge with Google contac: " + slave.Summary, EventType.Error);
            //        return;
            //    }					
            //}

            //TODO: convert to merge as opposed to replace

            #region Title/FileAs
        
            
            if (!string.IsNullOrEmpty(master.FileAs))
                slave.Title = master.FileAs;
            else if (!string.IsNullOrEmpty(master.FullName))
				slave.Title = master.FullName;
            else if (!string.IsNullOrEmpty(master.CompanyName))
				slave.Title = master.CompanyName;
            else if (slave.Emails.Count > 0 && !string.IsNullOrEmpty(slave.Emails[0].Address))
                slave.Title = slave.Emails[0].Address;
            else
                slave.Title = null;

			
            #endregion Title/FileAs

            #region Name
            Name name = new Name();            
            name.NamePrefix = master.Title;
            name.GivenName = master.FirstName;
            name.AdditonalName = master.MiddleName;
            name.FamilyName = master.LastName;
            name.NameSuffix = master.Suffix;

            name.FullName = master.FileAs; //Use the Google's full name to save a unique identifier. When saving the FullName, it always overwrites the Google Title
            slave.Name = name;
            #endregion Name

            #region Birtday
            if (master.Birthday.Equals(outlookDateNone)) //earlier also || master.Birthday.Year < 1900
                slave.ContactEntry.Birthday = null;
            else
                slave.ContactEntry.Birthday = master.Birthday.ToString("yyyy-MM-dd");
            #endregion Birthday

            slave.ContactEntry.Nickname = master.NickName;
            slave.Location = master.OfficeLocation;
            //Categories are synced separately in Syncronizer.OverwriteContactGroups: slave.Categories = master.Categories;
            slave.ContactEntry.Initials = master.Initials;
            //slave.ContactEntry.Language = master.Language;

			SetEmails(master, slave);

			SetAddresses(master, slave);
			
			SetPhoneNumbers(master, slave);
			
			SetCompanies(master, slave);

			SetIMs(master, slave);

            #region anniversary
            //First remove anniversary
            foreach (Event ev in slave.ContactEntry.Events)
            {
                if (ev.Relation != null && ev.Relation.Equals(relAnniversary))
                {
                    slave.ContactEntry.Events.Remove(ev);
                    break;
                }
            }
            //Then add it again if existing
            if (!master.Anniversary.Equals(outlookDateNone)) //earlier also || master.Birthday.Year < 1900
            {
                Event ev = new Event();
                ev.Relation = relAnniversary;
                ev.When = new When();
                ev.When.AllDay = true;
                ev.When.StartTime = master.Anniversary.Date;            
                slave.ContactEntry.Events.Add(ev);
            }
            #endregion anniversary

            #region spouse and child
            //First remove spouse and child
            foreach (Relation rel in slave.ContactEntry.Relations)
            {
                if (rel.Rel != null && (rel.Rel.Equals(relSpouse) || rel.Rel.Equals(relChild)))
                {
                    slave.ContactEntry.Relations.Remove(rel);
                    break;
                }
            }
            //Then add spouse again if existing
            if (!string.IsNullOrEmpty(master.Spouse))        
            {
                Relation rel = new Relation();
                rel.Rel = relSpouse;
                rel.Value = master.Spouse;                
                slave.ContactEntry.Relations.Add(rel);
            }
            //Then add children again if existing
            if (!string.IsNullOrEmpty(master.Children))               
            {
                Relation rel = new Relation();
                rel.Rel = relChild;
                rel.Value = master.Children;                
                slave.ContactEntry.Relations.Add(rel);
            }
            #endregion spouse and child

            #region HomePage
            slave.ContactEntry.Websites.Clear();
            //Just copy the first URL, because Outlook only has 1
            if (!string.IsNullOrEmpty(master.WebPage))
            {
                Website url = new Website();
                url.Href = master.WebPage;
                url.Rel = relHomePage;
                url.Primary = true;
                slave.ContactEntry.Websites.Add(url);
            }
            #endregion HomePage

            //CH - Fixed error with invalid xml being sent to google... This may need to be added to everything
            //slave.Content = String.Format("<![CDATA[{0}]]>", master.Body);
            //floriwan: Maybe better to just escape the XML instead of putting it in CDATA, because this causes a CDATA added to all my contacts
            if (!string.IsNullOrEmpty(master.Body))
                slave.Content = System.Security.SecurityElement.Escape(master.Body);
            else
                slave.Content = null;
		}

		public static void MergeContacts(Contact master, Outlook.ContactItem slave)
		{
			//// if no email or number, contact will be updated at each sync
			//if (master.Emails.Count == 0 && master.Phonenumbers.Count == 0)
            //    return;

            #region Title/FileAs
            if (!string.IsNullOrEmpty(master.Name.FullName))
				slave.FileAs = master.Name.FullName.Replace("\r\n", "\n").Replace("\n","\r\n"); //Replace twice to not replace a \r\n by \r\r\n. This is necessary because \r\n are saved as \n only to google
            else if (!string.IsNullOrEmpty(master.Title))
                slave.FileAs = master.Title.Replace("\r\n", "\n").Replace("\n","\r\n"); //Replace twice to not replace a \r\n by \r\r\n. This is necessary because \r\n are saved as \n only to google
            else if (master.Organizations.Count > 0 && !string.IsNullOrEmpty(master.Organizations[0].Name))
                slave.FileAs = master.Organizations[0].Name;
            else if (master.Emails.Count > 0 && !string.IsNullOrEmpty(master.Emails[0].Address))
				slave.FileAs = master.Emails[0].Address;
			else
			{
				if (!String.IsNullOrEmpty(slave.Email1Address))
				{
					Logger.Log("Google Contact '" + master.Summary + "' has neither name nor E-Mail address. Setting E-Mail address of Outlook contact: " + slave.Email1Address, EventType.Warning);
					master.Emails.Add(new EMail(slave.Email1Address));
					slave.FileAs = master.Emails[0].Address;
				}
				else
				{
					Logger.Log("Google Contact '" + master.Summary + "' has neither name nor E-Mail address. Cannot merge with Outlook contact: " + slave.FileAs, EventType.Error);
					return;
				}
            }
            #endregion Title/FileAs

            #region Name
            //slave.FullName = master.Name.FullName; //The Outlook fullName is automatically set, so don't assign it from Google
            slave.Title = master.Name.NamePrefix;
            slave.FirstName = master.Name.GivenName;
            slave.MiddleName = master.Name.AdditonalName;
            slave.LastName = master.Name.FamilyName;
            slave.Suffix = master.Name.NameSuffix;
            #endregion Name

            #region birthday
            DateTime birthday;
            DateTime.TryParse(master.ContactEntry.Birthday, out birthday);

            if (birthday != DateTime.MinValue)
            {
                if (!birthday.Date.Equals(slave.Birthday.Date)) //Only update if not already equal to avoid recreating the calendar item again and again
                    slave.Birthday = birthday.Date;
            }
            else
                slave.Birthday = outlookDateNone;
            #endregion birthday

            slave.NickName = master.ContactEntry.Nickname;
            slave.OfficeLocation = master.Location;
            //Categories are synced separately in Syncronizer.OverwriteContactGroups: slave.Categories = master.Categories;
            slave.Initials = master.ContactEntry.Initials;
            //slave.Language = master.ContactEntry.Language; //ToDo: Doesn't work yet with the Google API
            
			SetEmails(master, slave);

            #region phones
            //First delete the destination phone numbers
            slave.PrimaryTelephoneNumber = string.Empty;
            slave.HomeTelephoneNumber = string.Empty;
            slave.Home2TelephoneNumber = string.Empty;
            slave.BusinessTelephoneNumber = string.Empty;
            slave.Business2TelephoneNumber = string.Empty;
            slave.MobileTelephoneNumber = string.Empty;
            slave.BusinessFaxNumber = string.Empty;
            slave.HomeFaxNumber = string.Empty;
            slave.PagerNumber = string.Empty;
            //slave.RadioTelephoneNumber = string.Empty;
            slave.OtherTelephoneNumber = string.Empty;
            slave.CarTelephoneNumber = string.Empty;
            
			foreach (PhoneNumber phone in master.Phonenumbers)
			{                
				SetPhoneNumber(phone, slave);
            }

            //ToDo: Temporary cleanup algorithm to get rid of duplicate primary phone numbers
            //Can be removed once the contacts are clean for all users:
            if (!String.IsNullOrEmpty(slave.PrimaryTelephoneNumber))
            {
                if (slave.PrimaryTelephoneNumber.Equals(slave.MobileTelephoneNumber))
                {
                    slave.PrimaryTelephoneNumber = String.Empty;
                    if (slave.MobileTelephoneNumber.Equals(slave.HomeTelephoneNumber) ||
                        slave.MobileTelephoneNumber.Equals(slave.Home2TelephoneNumber) ||
                        slave.MobileTelephoneNumber.Equals(slave.BusinessTelephoneNumber) ||
                        slave.MobileTelephoneNumber.Equals(slave.Business2TelephoneNumber) ||
                        slave.MobileTelephoneNumber.Equals(slave.HomeFaxNumber) ||
                        slave.MobileTelephoneNumber.Equals(slave.BusinessFaxNumber) ||
                        slave.MobileTelephoneNumber.Equals(slave.OtherTelephoneNumber) ||
                        slave.MobileTelephoneNumber.Equals(slave.PagerNumber) ||
                        slave.MobileTelephoneNumber.Equals(slave.CarTelephoneNumber))
                    {
                        slave.MobileTelephoneNumber = String.Empty;
                    }

                }
                else if (slave.PrimaryTelephoneNumber.Equals(slave.HomeTelephoneNumber) ||
                    slave.PrimaryTelephoneNumber.Equals(slave.Home2TelephoneNumber) ||
                    slave.PrimaryTelephoneNumber.Equals(slave.BusinessTelephoneNumber) ||
                    slave.PrimaryTelephoneNumber.Equals(slave.Business2TelephoneNumber) ||
                    slave.PrimaryTelephoneNumber.Equals(slave.HomeFaxNumber) ||
                    slave.PrimaryTelephoneNumber.Equals(slave.BusinessFaxNumber) ||
                    slave.PrimaryTelephoneNumber.Equals(slave.OtherTelephoneNumber) ||
                    slave.PrimaryTelephoneNumber.Equals(slave.PagerNumber) ||
                    slave.PrimaryTelephoneNumber.Equals(slave.CarTelephoneNumber))
                {
                    //Reset primary TelephoneNumber because it is duplicate
                    slave.PrimaryTelephoneNumber = string.Empty;
                }

            }

            #endregion phones


            #region addresses
            slave.HomeAddress = string.Empty;
            slave.HomeAddressStreet = string.Empty;
            slave.HomeAddressCity = string.Empty;
            slave.HomeAddressPostalCode = string.Empty;
            slave.HomeAddressCountry = string.Empty;

            slave.BusinessAddress = string.Empty;
            slave.BusinessAddressStreet = string.Empty;
            slave.BusinessAddressCity = string.Empty;
            slave.BusinessAddressPostalCode = string.Empty;
            slave.BusinessAddressCountry = string.Empty;

            slave.OtherAddress = string.Empty;
            slave.OtherAddressStreet = string.Empty;
            slave.OtherAddressCity = string.Empty;
            slave.OtherAddressPostalCode = string.Empty;
            slave.OtherAddressCountry = string.Empty;

            slave.SelectedMailingAddress = Microsoft.Office.Interop.Outlook.OlMailingAddress.olNone;
			foreach (StructuredPostalAddress address in master.PostalAddresses)
			{
				SetPostalAddress(address, slave);
            }
            #endregion addresses

            #region companies
            slave.Companies = string.Empty;
            slave.CompanyName = string.Empty;
            slave.JobTitle = string.Empty;
            slave.Department = string.Empty;
			foreach (Organization company in master.Organizations)
			{
				if (string.IsNullOrEmpty(company.Name) && string.IsNullOrEmpty(company.Title) && string.IsNullOrEmpty(company.Department))
					continue;

				if (company.Primary || company.Equals(master.Organizations[0]))
                {//Per default copy the first company, but if there is a primary existing, use the primary
					slave.CompanyName = company.Name;
                    slave.JobTitle = company.Title;
                    slave.Department = company.Department;
                }
				if (!string.IsNullOrEmpty(slave.Companies))
					slave.Companies += "; ";
				slave.Companies += company.Name;
			}
            #endregion companies

            #region IM
            slave.IMAddress = string.Empty;
			foreach (IMAddress im in master.IMs)
			{
				if (!string.IsNullOrEmpty(slave.IMAddress))
					slave.IMAddress += "; ";
				if (!string.IsNullOrEmpty(im.Protocol))
					slave.IMAddress += im.Protocol + ": " + im.Address;
				slave.IMAddress += im.Address;
			}        
            #endregion IM

            #region anniversary
            bool found = false;
            foreach (Event ev in master.ContactEntry.Events)
            {
                if (ev.Relation != null && ev.Relation.Equals(relAnniversary))
                {
                    if (!ev.When.StartTime.Date.Equals(slave.Anniversary.Date)) //Only update if not already equal to avoid recreating the calendar item again and again
                        slave.Anniversary = ev.When.StartTime.Date;
                    found = true;
                    break;
                }
            }
            if (!found)
                slave.Anniversary = outlookDateNone; //set to empty in the end
            #endregion anniversary

            #region spouse and child
            slave.Children = string.Empty;
            slave.Spouse = string.Empty;
            foreach (Relation rel in master.ContactEntry.Relations)
            {
                if (rel.Rel != null && rel.Rel.Equals(relChild))
                    slave.Children = rel.Value;
                if (rel.Rel != null && rel.Rel.Equals(relSpouse))
                    slave.Spouse = rel.Value;
            }
            #endregion spouse and child

            foreach (Website website in master.ContactEntry.Websites)
            {               
                if (website.Primary || website.Equals(master.ContactEntry.Websites[0]))
                {//Per default copy the first website, but if there is a primary existing, use the primary
                    slave.WebPage = master.ContactEntry.Websites[0].Href; 
                }               
            }
                

			slave.Body = master.Content;
		}

		public static void SetEmails(Contact source, Outlook.ContactItem destination)
		{
            destination.Email1Address = string.Empty;
            destination.Email1DisplayName = string.Empty;

            destination.Email2Address = string.Empty;
            destination.Email2DisplayName = string.Empty;

            destination.Email3Address = string.Empty;
            destination.Email3DisplayName = string.Empty;

			if (source.Emails.Count > 0)
			{
				destination.Email1Address = source.Emails[0].Address;
				destination.Email1DisplayName = source.Emails[0].Label;
			}            

			if (source.Emails.Count > 1)
            {
				destination.Email2Address = source.Emails[1].Address;
                destination.Email2DisplayName = source.Emails[1].Label;
            }
            
			if (source.Emails.Count > 2)
            {
				destination.Email3Address = source.Emails[2].Address;
                destination.Email3DisplayName = source.Emails[2].Label;
            }
            
		}

	}
}
