using Microsoft.AspNetCore.Identity;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace ServiceDesk.Entities
{
    public enum ComplainType
    {
        [Description("Общая проблема")]
        [Display(Name = "Общая проблема")]
        General =0,
        [Description("Техническая проблема")]
        [Display(Name = "Техническая проблема")]
        Technical =1,
        [Description("Проблема доступа")]
        [Display(Name = "Проблема доступа")]
        AccessIssue =2,
        [Description("Сообщение об ошибке")]
        [Display(Name = "Сообщение об ошибке")]
        BugReport =3,
        [Description("Другое")]
        [Display(Name = "Другое")]
        Other = 4
    }
}