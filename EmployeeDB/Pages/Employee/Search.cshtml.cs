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

    [BindProperty]
    public EmployeeViewModel Employee { get; set; }

    public string ErrorMessage { get; set; }
    public string SuccessMessage { get; set; }

    private readonly string connectionString = "Data Source=MiRiYaM-LAPTOP\\SQLEXPRESS02;Integrated Security=True";

    public void OnGet()
    {
        LoadBranches();
        SearchEmployees();
    }

    public IActionResult OnPostAdd()
    {
        try
        {
            if (Employee != null)
            {
                AddEmployee(Employee);
                SuccessMessage = "Employee added successfully.";
            }
            else
            {
                ErrorMessage = "Invalid employee data.";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error adding employee: {ex.Message}";
        }

        LoadBranches();
        SearchEmployees();
        return Page();
    }

    public IActionResult OnPostUpdate()
    {
        try
        {
            if (Employee != null && Employee.EmployeeNo > 0)
            {
                UpdateEmployee(Employee);
                SuccessMessage = "Employee updated successfully.";
            }
            else
            {
                ErrorMessage = "Invalid employee data.";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error updating employee: {ex.Message}";
        }

        LoadBranches();
        SearchEmployees();
        return Page();
    }

    public IActionResult OnPostDelete(int employeeNo)
    {
        try
        {
            DeleteEmployee(employeeNo);
            SuccessMessage = "Employee deleted successfully.";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error deleting employee: {ex.Message}";
        }

        LoadBranches();
        SearchEmployees();
        return Page();
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

    private void AddEmployee(EmployeeViewModel employee)
    {
        using (var connection = new SqlConnection(connectionString))
        {
            connection.Open();
            var command = new SqlCommand(@"INSERT INTO EMPLOYEE (BRANCH_CODE, NAME, DOB, STATUS)
                                           VALUES (@BranchCode, @Name, @DOB, @Status)", connection);
            command.Parameters.AddWithValue("@BranchCode", employee.BranchName);
            command.Parameters.AddWithValue("@Name", employee.Name);
            command.Parameters.AddWithValue("@DOB", DateTime.Now.AddYears(-employee.Age));
            command.Parameters.AddWithValue("@Status", employee.Status);

            command.ExecuteNonQuery();
        }
    }

    private void UpdateEmployee(EmployeeViewModel employee)
    {
        using (var connection = new SqlConnection(connectionString))
        {
            connection.Open();
            var command = new SqlCommand(@"UPDATE EMPLOYEE SET 
                                           BRANCH_CODE = @BranchCode,
                                           NAME = @Name,
                                           DOB = @DOB,
                                           STATUS = @Status
                                           WHERE EMPNO = @EmployeeNo", connection);
            command.Parameters.AddWithValue("@BranchCode", employee.BranchName);
            command.Parameters.AddWithValue("@Name", employee.Name);
            command.Parameters.AddWithValue("@DOB", DateTime.Now.AddYears(-employee.Age));
            command.Parameters.AddWithValue("@Status", employee.Status);
            command.Parameters.AddWithValue("@EmployeeNo", employee.EmployeeNo);

            command.ExecuteNonQuery();
        }
    }

    private void DeleteEmployee(int employeeNo)
    {
        using (var connection = new SqlConnection(connectionString))
        {
            connection.Open();
            var command = new SqlCommand("DELETE FROM EMPLOYEE WHERE EMPNO = @EmployeeNo", connection);
            command.Parameters.AddWithValue("@EmployeeNo", employeeNo);

            command.ExecuteNonQuery();
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
