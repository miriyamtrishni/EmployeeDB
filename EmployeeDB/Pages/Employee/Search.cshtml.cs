using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
public class EmployeeSearchModel : PageModel
{
    public List<Branch> Branches { get; set; } = new List<Branch>();
    public List<EmployeeViewModel> Employees { get; set; } = new List<EmployeeViewModel>();

    [BindProperty(SupportsGet = true)]
    public string BranchName { get; set; }

    [BindProperty(SupportsGet = true)]
    public string Status { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? Age { get; set; }

    private readonly string connectionString = "Data Source=.\\sqlexpress;Initial Catalog=Employee;Integrated Security=True";

    public void OnGet()
    {
        LoadBranches();
        SearchEmployees();
    }

    private void LoadBranches()
    {
        using (var connection = new SqlConnection(connectionString))
        {
            connection.Open();
            var command = new SqlCommand("SELECT BRANCH_NAME FROM BRANCHES", connection);
            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    Branches.Add(new Branch
                    {
                        BRANCH_NAME = reader["BRANCH_NAME"].ToString()
                    });
                }
            }
        }
    }

    private void SearchEmployees()
    {
        using (var connection = new SqlConnection(connectionString))
        {
            connection.Open();

            string query = @"SELECT B.BRANCH_NAME, E.EMPNO, E.NAME, 
                             DATEDIFF(YEAR, E.DOB, GETDATE()) AS AGE, E.STATUS
                             FROM EMPLOYEE E
                             JOIN BRANCHES B ON E.BRANCH_CODE = B.BRANCH_CODE
                             WHERE (@BranchName IS NULL OR B.BRANCH_NAME = @BranchName)
                             AND (@Status IS NULL OR E.STATUS = @Status)
                             AND (@Age IS NULL OR DATEDIFF(YEAR, E.DOB, GETDATE()) > @Age)";

            var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@BranchName", (object)BranchName ?? DBNull.Value);
            command.Parameters.AddWithValue("@Status", (object)Status ?? DBNull.Value);
            command.Parameters.AddWithValue("@Age", (object)Age ?? DBNull.Value);

            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    Employees.Add(new EmployeeViewModel
                    {
                        BranchName = reader["BRANCH_NAME"].ToString(),
                        EmployeeNo = Convert.ToInt32(reader["EMPNO"]),
                        Name = reader["NAME"].ToString(),
                        Age = Convert.ToInt32(reader["AGE"]),
                        Status = reader["STATUS"].ToString()
                    });
                }
            }
        }
    }
}

public class Branch
{
    public string BRANCH_NAME { get; set; }
}

public class EmployeeViewModel
{
    public string BranchName { get; set; }
    public int EmployeeNo { get; set; }
    public string Name { get; set; }
    public int Age { get; set; }
    public string Status { get; set; }
}
