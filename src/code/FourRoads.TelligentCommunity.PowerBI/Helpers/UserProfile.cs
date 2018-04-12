using System;
using System.Collections.Generic;
using Telligent.Evolution.Extensibility.Api.Entities.Version1;
using Telligent.Evolution.Extensibility.Api.Version1;

namespace FourRoads.TelligentCommunity.PowerBI.Helpers
{
    public static class UserProfile
    {
        public static IEnumerable<UserProfileField> GetUserProfileFields()
        {
            List<UserProfileField> results = new List<UserProfileField>();

            results.Add(new UserProfileField() { Name = "DisplayName" , Title = "Display Name", FieldType = new UserProfileFieldType() { Name = "string" } });

            // this does not work in 10.1 as when called from ConfigurationOptions 
            // 
            // Apis.Get<IUserProfileFields>() returns null .... but PublicApi.UserProfileFields works OK 
            foreach (var field in PublicApi.UserProfileFields.List(new UserProfileFieldsListOptions() { PageIndex = 0, PageSize = int.MaxValue }))
            {
                results.Add(field);
            }

            foreach (UserProfileField field in results)
            {
                yield return field;
            }
        }

        public static string GetDataType(UserProfileField field)
        {
            string dataType = string.Empty;

            if (field.FieldType == null || string.IsNullOrWhiteSpace(field.FieldType.Name))
            {
                switch (field.Name)
                {
                    case "core_Birthday":
                        dataType = "DateTime";
                        break;
                    default:
                        dataType = "string";
                        break;
                }
            }
            else
            {
                switch (field.FieldType.Name)
                {
                    case "DateTime":
                        dataType = "DateTime";
                        break;
                    default:
                        dataType = "string";
                        break;
                }
            }

            return dataType;
        }

        public static string ExtractUserProfileValue(User user, UserProfileField field)
        {
            string dataType = GetDataType(field);
            string fieldValue = "";

            if (user.ProfileFields[field.Name] != null)
            {
                fieldValue = user.ProfileFields[field.Name].Value;
            }
            else
            {
                // get values for other core fields
                switch (field.Name)
                {
                    case "DisplayName":
                        fieldValue = user.DisplayName;
                        break;
                    default:
                        fieldValue = string.Empty;
                        break;
                }
            }

            switch (dataType)
            {
                case "DateTime":
                    return $"\"{FormatDate("yyyy-MM-dd HH:mm:ss", fieldValue)}\"";
                default:
                    return $"\"{fieldValue}\"";
            }
        }

        public static string FormatDate(string format = "yyyy-MM-ddTHH:mm:ssZ", string date = null)
        {
            DateTime dateTime;

            if (!string.IsNullOrWhiteSpace(date))
            {
                try
                {
                    dateTime = DateTime.ParseExact(date, format, null);
                }
                catch (Exception)
                {
                    dateTime = DateTime.Now;
                }
            }
            else
            {
                dateTime = DateTime.Now;
            }

            return dateTime.ToString("MM/dd/yyyy HH:mm:ss");
        }

        public static string FormatDateTime(DateTime? dateTime)
        {

            if (dateTime == null)
            {
                return string.Empty;
            }

            return dateTime.Value.ToString("MM/dd/yyyy HH:mm:ss");
        }


    }
}
