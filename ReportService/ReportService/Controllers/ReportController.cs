using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using ReportService.Domain;

namespace ReportService.Controllers
{
    [Route("api/[controller]")]
    public class ReportController : Controller
    {
        [HttpGet]
        [Route("{year}/{month}")]
        public IActionResult Download(int year, int month)
        {
            var actions = new List<(Action<Employee, Report>, Employee)>();
            var deps = new Dictionary<string, List<Employee>>();

            var report = new Report() {
                S = MonthNameResolver.MonthName.GetName(year, month) + " " + year
            };
            var connString = "Host=192.168.99.100;Username=postgres;Password=1;Database=employee";                       

            var conn = new NpgsqlConnection(connString);
            conn.Open();
            var cmd = new NpgsqlCommand(@"SELECT e.name, e.inn, d.name
                                          from emps e
                                          left join deps d on e.departmentid = d.id
                                          where d.active = true 
                                            and e.departmentid is not null 
                                        ", conn);
            var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                var depName = reader.GetString(2);
                if (!deps.ContainsKey(depName))
                {
                    deps[depName] = new List<Employee>();
                };

                var emp = new Employee()
                {
                    Name = reader.GetString(0),
                    Inn = reader.GetString(1),
                    Department = depName
                };
                emp.BuhCode = EmpCodeResolver.GetCode(emp.Inn).Result;
                emp.Salary = emp.Salary();
                deps[depName].Add(emp);
            }
            conn.Close();

            var totalAll = 0;
            foreach (var dep in deps)
            {
                var depName = dep.Key;
                actions.Add((new ReportFormatter(null).NL, new Employee()));
                actions.Add((new ReportFormatter(null).WL, new Employee()));
                actions.Add((new ReportFormatter(null).NL, new Employee()));                
                actions.Add((new ReportFormatter(null).WD, new Employee() { Department = depName }));
                actions.Add((new ReportFormatter(null).NL, null));

                var empDeplist = dep.Value;
                var totalByDepartment = 0;
                for (int i = 0; i < empDeplist.Count(); i++)
                {
                    actions.Add((new ReportFormatter(empDeplist[i]).NL, empDeplist[i]));
                    actions.Add((new ReportFormatter(empDeplist[i]).WE, empDeplist[i]));
                    actions.Add((new ReportFormatter(empDeplist[i]).WT, empDeplist[i]));
                    actions.Add((new ReportFormatter(empDeplist[i]).WS, empDeplist[i]));
                    totalByDepartment += empDeplist[i].Salary;
                }

                totalAll += totalByDepartment;
                actions.Add((new ReportFormatter(null).NL, null));
                actions.Add((new ReportFormatter(null).NL, null));
                actions.Add((new ReportFormatter(null).WTD, new Employee() { TotalByDepartment = totalByDepartment }));
                                
                actions.Add((new ReportFormatter(null).NL, null));
                actions.Add((new ReportFormatter(null).WL, null));
            }
            actions.Add((new ReportFormatter(null).NL, null));
            actions.Add((new ReportFormatter(null).NL, null));
            actions.Add((new ReportFormatter(null).WTA, new Employee() { TotalAll = totalAll }));

            foreach (var act in actions)
            {
                act.Item1(act.Item2, report);
            }
            report.Save();
            var file = report.ReadFile();
            var response = File(file, "application/octet-stream", "report.txt");
            return response;
        }
    }
}
