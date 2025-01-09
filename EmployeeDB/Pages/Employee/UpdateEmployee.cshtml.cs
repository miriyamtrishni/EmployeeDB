using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;

namespace EmployeeDB.Pages.Employee
{
    public class UpdateEmployeeModel : PageModel
    {
        private readonly IConfiguration _configuration;
        private readonly string _connectionString;
        private readonly ILogger<UpdateEmployeeModel> _logger;

        public UpdateEmployeeModel(IConfiguration configuration, ILogger<UpdateEmployeeModel> logger)
        {
            _configuration = configuration;
            _connectionString = _configuration.GetConnectionString("DefaultConnection");
            _logger = logger;
        }

        // Properties for form data
        [BindProperty]
        public EmployeeDetails Employee { get; set; }

        [BindProperty]
        public string BranchCode { get; set; }

        public List<Branch> Branches { get; private set; }

        // For dropdowns
        public SelectList BranchSelectList { get; set; }
        public SelectList StatusSelectList { get; set; }

        public string ErrorMessage { get; set; }

        public void OnGet(int empNo)
        {
            LoadBranches();
            LoadEmployeeDetails(empNo);
            // Initialize SelectLists
            BranchSelectList = new SelectList(Branches, "BranchCode", "BranchName", BranchCode);
            StatusSelectList = new SelectList(new List<string> { "Active", "Inactive" }, selectedValue: Employee.Status);
        }

        public IActionResult OnPost()
        {
            if (!ModelState.IsValid)
            {
                LoadBranches();
                // Recreate SelectLists
                BranchSelectList = new SelectList(Branches, "BranchCode", "BranchName", BranchCode);
                StatusSelectList = new SelectList(new List<string> { "Active", "Inactive" }, selectedValue: Employee.Status);
                return Page();
            }

            try
            {
                // Input Validation
                if (Employee.EMPNO <= 0)
                {
                    ErrorMessage = "Invalid Employee Number.";
                    LoadBranches();
                    // Recreate SelectLists
                    BranchSelectList = new SelectList(Branches, "BranchCode", "BranchName", BranchCode);
                    StatusSelectList = new SelectList(new List<string> { "Active", "Inactive" }, selectedValue: Employee.Status);
                    return Page();
                }

                if (string.IsNullOrWhiteSpace(BranchCode))
                {
                    ErrorMessage = "Branch is required.";
                    LoadBranches();
                    BranchSelectList = new SelectList(Branches, "BranchCode", "BranchName", BranchCode);
                    StatusSelectList = new SelectList(new List<string> { "Active", "Inactive" }, selectedValue: Employee.Status);
                    return Page();
                }

                if (string.IsNullOrWhiteSpace(Employee.Name))
                {
                    ErrorMessage = "Employee name is required.";
                    LoadBranches();
                    BranchSelectList = new SelectList(Branches, "BranchCode", "BranchName", BranchCode);
                    StatusSelectList = new SelectList(new List<string> { "Active", "Inactive" }, selectedValue: Employee.Status);
                    return Page();
                }

                if ((DateTime.Now - Employee.DOB).TotalDays / 365.25 < 18)
                {
                    ErrorMessage = "Employee must be at least 18 years old.";
                    LoadBranches();
                    BranchSelectList = new SelectList(Branches, "BranchCode", "BranchName", BranchCode);
                    StatusSelectList = new SelectList(new List<string> { "Active", "Inactive" }, selectedValue: Employee.Status);
                    return Page();
                }

                UpdateEmployeeInDatabase();
                TempData["SuccessMessage"] = "Employee updated successfully.";
                return RedirectToPage("./EmployeeSearch");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating employee.");
                ErrorMessage = $"Error updating employee: {ex.Message}";
                LoadBranches();
                BranchSelectList = new SelectList(Branches, "BranchCode", "BranchName", BranchCode);
                StatusSelectList = new SelectList(new List<string> { "Active", "Inactive" }, selectedValue: Employee.Status);
                return Page();
            }
        }

        private void LoadBranches()
        {
            Branches = new List<Branch>();

            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    connection.Open();
                    var command = new SqlCommand("SELECT BRANCH_CODE, BRANCH_NAME FROM BRANCHES", connection);

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            Branches.Add(new Branch
                            {
                                BranchCode = reader["BRANCH_CODE"].ToString(),
                                BranchName = reader["BRANCH_NAME"].ToString()
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading branches.");
                ErrorMessage = $"Error loading branches: {ex.Message}";
            }
        }

        private void LoadEmployeeDetails(int empNo)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    connection.Open();
                    var command = new SqlCommand("SELECT * FROM EMPLOYEE WHERE EMPNO = @EmpNo", connection);
                    command.Parameters.AddWithValue("@EmpNo", empNo);

                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            Employee = new EmployeeDetails
                            {
                                EMPNO = Convert.ToInt32(reader["EMPNO"]),
                                Name = reader["NAME"].ToString(),
                                DOB = Convert.ToDateTime(reader["DOB"]),
                                BranchCode = reader["BRANCH_CODE"].ToString(),
                                Status = reader["STATUS"].ToString()
                            };
                            BranchCode = Employee.BranchCode;
                        }
                        else
                        {
                            ErrorMessage = "Employee not found.";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading employee details.");
                ErrorMessage = $"Error loading employee details: {ex.Message}";
            }
        }

        private void UpdateEmployeeInDatabase()
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    connection.Open();
                    var command = new SqlCommand(
                        @"UPDATE EMPLOYEE 
                          SET NAME = @Name, DOB = @DOB, BRANCH_CODE = @BranchCode, STATUS = @Status
                          WHERE EMPNO = @EmpNo", connection);

                    command.Parameters.AddWithValue("@Name", Employee.Name);
                    command.Parameters.AddWithValue("@DOB", Employee.DOB);
                    command.Parameters.AddWithValue("@BranchCode", BranchCode);
                    command.Parameters.AddWithValue("@Status", Employee.Status);
                    command.Parameters.AddWithValue("@EmpNo", Employee.EMPNO);

                    int rowsAffected = command.ExecuteNonQuery();
                    if (rowsAffected == 0)
                    {
                        ErrorMessage = "Update failed. Employee not found.";
                        _logger.LogWarning("Update failed. Employee with EMPNO {EmpNo} not found.", Employee.EMPNO);
                    }
                }
            }
            catch (SqlException ex)
            {
                throw new Exception("Error updating employee: " + ex.Message);
            }
        }

        public class EmployeeDetails
        {
            public int EMPNO { get; set; }
            public string Name { get; set; }
            public DateTime DOB { get; set; }
            public string BranchCode { get; set; }
            public string Status { get; set; }
        }

        public class Branch
        {
            public string BranchCode { get; set; }
            public string BranchName { get; set; }
        }
    }
}