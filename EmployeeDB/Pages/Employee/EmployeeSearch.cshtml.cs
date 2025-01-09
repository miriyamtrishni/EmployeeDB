using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;

namespace EmployeeDB.Pages.Employee
{
    public class EmployeeSearchModel : PageModel
    {
        private readonly IConfiguration _configuration;
        private readonly string _connectionString;

        public EmployeeSearchModel(IConfiguration configuration)
        {
            _configuration = configuration;
            _connectionString = _configuration.GetConnectionString("DefaultConnection");
        }

        public List<Branch> Branches { get; set; } = new List<Branch>();
        public List<EmployeeViewModel> Employees { get; set; } = new List<EmployeeViewModel>();

        [BindProperty(SupportsGet = true)]
        public string BranchName { get; set; }

        [BindProperty(SupportsGet = true)]
        public string Status { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? Age { get; set; }

        // For dropdowns
        public SelectList BranchSelectList { get; set; }
        public SelectList StatusSelectList { get; set; }

        public void OnGet()
        {
            LoadBranches();
            SearchEmployees();
            // Initialize SelectLists
            BranchSelectList = new SelectList(Branches, "BRANCH_NAME", "BRANCH_NAME", BranchName);
            StatusSelectList = new SelectList(new List<string> { "", "Active", "Inactive" }, selectedValue: Status);
        }

        public IActionResult OnPostDelete(int id)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    connection.Open();
                    var command = new SqlCommand("DELETE FROM EMPLOYEE WHERE EMPNO = @Id", connection);
                    command.Parameters.AddWithValue("@Id", id);
                    int rowsAffected = command.ExecuteNonQuery();

                    if (rowsAffected > 0)
                    {
                        TempData["SuccessMessage"] = "Employee deleted successfully.";
                    }
                    else
                    {
                        TempData["ErrorMessage"] = "Employee not found.";
                    }
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error deleting employee: {ex.Message}";
            }

            // Refresh the page after deleting the record
            return RedirectToPage();
        }

        private void LoadBranches()
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
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
            catch (Exception ex)
            {
                // Log the exception (not shown here for brevity)
                // Optionally, set an error message to display in the UI
                TempData["ErrorMessage"] = $"Error loading branches: {ex.Message}";
            }
        }

        private void SearchEmployees()
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
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
                    command.Parameters.AddWithValue("@BranchName", string.IsNullOrEmpty(BranchName) ? (object)DBNull.Value : BranchName);
                    command.Parameters.AddWithValue("@Status", string.IsNullOrEmpty(Status) ? (object)DBNull.Value : Status);
                    command.Parameters.AddWithValue("@Age", Age.HasValue ? (object)Age.Value : DBNull.Value);

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
            catch (Exception ex)
            {
                // Log the exception (not shown here for brevity)
                // Optionally, set an error message to display in the UI
                TempData["ErrorMessage"] = $"Error searching employees: {ex.Message}";
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
}