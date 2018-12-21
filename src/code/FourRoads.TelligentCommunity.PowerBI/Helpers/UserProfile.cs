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

            results.Add(new UserProfileField() { Name = "DisplayName", Title = "Display Name", FieldType = new UserProfileFieldType() { Name = "string" } });
            results.Add(new UserProfileField() { Name = "AgeGroup", Title = "Age Group", FieldType = new UserProfileFieldType() { Name = "string" } });

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
                switch (field.Name)
                {
                    case "core_Gender":
                        fieldValue = user.Gender;
                        break;
                    default:
                        fieldValue = user.ProfileFields[field.Name].Value;
                        break;
                }
            }
            else
            {
                // get values for other core fields
                switch (field.Name)
                {
                    case "DisplayName":
                        fieldValue = user.DisplayName;
                        break;
                    case "AgeGroup":
                        if (user.Birthday.HasValue && user.Birthday.Value.Year > 1)
                        {
                            var age = CalculateAge((DateTime)user.Birthday);
                            if (age < 20)
                            {
                                fieldValue = "under 20";
                            }
                            else if (age >= 20 && age < 30)
                            {
                                fieldValue = "20 to 30";
                            }
                            else if (age >= 30 && age < 40)
                            {
                                fieldValue = "30 to 40";
                            }
                            else if (age >= 40 && age < 50)
                            {
                                fieldValue = "40 to 50";
                            }
                            else if (age >= 50 && age < 60)
                            {
                                fieldValue = "50 to 60";
                            }
                            else if (age >= 60 && age < 70)
                            {
                                fieldValue = "60 to 70";
                            }
                            else if (age >= 70)
                            {
                                fieldValue = "70 and over";
                            }
                        }
                        break;
                    default:
                        fieldValue = "";
                        break;
                }
            }

            switch (dataType)
            {
                case "DateTime":
                    return $"\"{FormatDate("yyyy-MM-dd HH:mm:ss", fieldValue)}\"";
                default:
                    if (string.IsNullOrWhiteSpace(fieldValue) || fieldValue.Equals("NotSet", StringComparison.InvariantCultureIgnoreCase))
                    {
                        fieldValue = "Unknown";
                    }

                    return $"\"{fieldValue}\"";
            }
        }

        /// <summary>  
        /// For calculating only age  
        /// </summary>  
        /// <param name="dateOfBirth">Date of birth</param>  
        /// <returns> age e.g. 26</returns>  
        private static int CalculateAge(DateTime dateOfBirth)
        {
            int age = 0;
            age = DateTime.Now.Year - dateOfBirth.Year;
            if (DateTime.Now.DayOfYear < dateOfBirth.DayOfYear)
                age = age - 1;

            return age;
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
