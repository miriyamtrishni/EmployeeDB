using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;

public class CreateEmployeeModel : PageModel
{
    public int NextEmpNo { get; set; }
    public List<BranchViewModel> Branches { get; set; } = new List<BranchViewModel>();

    [BindProperty]
    public string BranchCode { get; set; }

    [BindProperty]
    public string Name { get; set; }

    [BindProperty]
    public DateTime DOB { get; set; }

    public string ErrorMessage { get; set; }
    public bool IsSuccess { get; set; }

    private readonly string connectionString = "Data Source=.\\sqlexpress;Initial Catalog=Employee;Integrated Security=True";

    public void OnGet()
    {
        try
        {
            LoadBranches();
            GetNextEmpNo();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"An error occurred: {ex.Message}";
        }
    }

    public IActionResult OnPost()
    {
        try
        {
            LoadBranches();
            GetNextEmpNo();

            if (string.IsNullOrWhiteSpace(BranchCode))
            {
                ErrorMessage = "Branch Code is required.";
                return Page();
            }

            if ((DateTime.Now - DOB).TotalDays / 365.25 < 18)
            {
                ErrorMessage = "Employee must be at least 18 years old.";
                return Page();
            }

            AddEmployee();
            IsSuccess = true;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"An error occurred: {ex.Message}";
            return Page();
        }

        return Page();
    }

    private void LoadBranches()
    {
        try
        {
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                var command = new SqlCommand("SELECT BRANCH_CODE, BRANCH_NAME FROM BRANCHES", connection);
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        Branches.Add(new BranchViewModel
                        {
                            BRANCH_CODE = reader["BRANCH_CODE"].ToString(),
                            BRANCH_NAME = reader["BRANCH_NAME"].ToString()
                        });
                    }
                }
            }
        }
        catch (SqlException ex)
        {
            throw new Exception("Error loading branches: " + ex.Message);
        }
    }

    private void GetNextEmpNo()
    {
        try
        {
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                var command = new SqlCommand("SELECT ISNULL(MAX(EMPNO), 0) + 1 AS NextEmpNo FROM EMPLOYEE", connection);
                NextEmpNo = (int)command.ExecuteScalar();
            }
        }
        catch (SqlException ex)
        {
            throw new Exception("Error getting next employee number: " + ex.Message);
        }
    }

    private void AddEmployee()
    {
        try
        {
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                var command = new SqlCommand(@"INSERT INTO EMPLOYEE (EMPNO, BRANCH_CODE, NAME, DOB, STATUS)
                                               VALUES (@EmpNo, @BranchCode, @Name, @DOB, 'Active')", connection);
                command.Parameters.AddWithValue("@EmpNo", NextEmpNo);
                command.Parameters.AddWithValue("@BranchCode", BranchCode);
                command.Parameters.AddWithValue("@Name", Name);
                command.Parameters.AddWithValue("@DOB", DOB);

                command.ExecuteNonQuery();
            }
        }
        catch (SqlException ex)
        {
            throw new Exception("Error adding employee: " + ex.Message);
        }
    }
}

public class BranchViewModel
{
    public string BRANCH_CODE { get; set; }
    public string BRANCH_NAME { get; set; }
}
