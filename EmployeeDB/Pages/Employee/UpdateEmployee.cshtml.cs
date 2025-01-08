using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Data.SqlClient;

public class UpdateEmployeeModel : PageModel
{
    [BindProperty]
    public EmployeeViewModel Employee { get; set; }

    private readonly string connectionString = "Data Source=MiRiYaM-LAPTOP\\SQLEXPRESS02;Integrated Security=True";

    public void OnGet(int id)
    {
        using (var connection = new SqlConnection(connectionString))
        {
            connection.Open();
            var command = new SqlCommand("SELECT EMPNO, NAME, STATUS FROM EMPLOYEE WHERE EMPNO = @Id", connection);
            command.Parameters.AddWithValue("@Id", id);

            using (var reader = command.ExecuteReader())
            {
                if (reader.Read())
                {
                    Employee = new EmployeeViewModel
                    {
                        EmployeeNo = Convert.ToInt32(reader["EMPNO"]),
                        Name = reader["NAME"].ToString(),
                        Status = reader["STATUS"].ToString()
                    };
                }
            }
        }
    }

    public IActionResult OnPost()
    {
        if (!ModelState.IsValid)
        {
            return Page(); // Return the same page if form data is invalid
        }

        using (var connection = new SqlConnection(connectionString))
        {
            connection.Open();
            var command = new SqlCommand("UPDATE EMPLOYEE SET NAME = @Name, STATUS = @Status WHERE EMPNO = @Id", connection);

            // Add parameters to the query
            command.Parameters.AddWithValue("@Name", Employee.Name);
            command.Parameters.AddWithValue("@Status", Employee.Status);
            command.Parameters.AddWithValue("@Id", Employee.EmployeeNo);

            command.ExecuteNonQuery();
        }

        // Redirect to the main search page
        return RedirectToPage("/EmployeeSearch");
    }
}

public class EmployeeViewModel
{
    public int EmployeeNo { get; set; }
    public string Name { get; set; }
    public string Status { get; set; }
}
