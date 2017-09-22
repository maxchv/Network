using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using WebServer;
using static System.Configuration.ConfigurationSettings;

namespace WebApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        private HttpServer _httpServer;

        public MainWindow()
        {
            InitializeComponent();
        }

        private async void StartStopServerButton_OnClick(object sender, RoutedEventArgs e)
        {
            try
            {
                if (StartStopServerButton.Content.ToString().Equals("Запустить сервер"))
                {
                    StartStopServerButton.Content = "Остановить сервер";
                    Port.IsEnabled = false;

                    _httpServer = new HttpServer(Convert.ToInt32(Port.Value));
                    _httpServer.WebAppEvent += HttpServerOnWebAppEvent;
                    await _httpServer.RunAsync();
                }
                else if (StartStopServerButton.Content.ToString().Equals("Остановить сервер"))
                {
                    StartStopServerButton.Content = "Запустить сервер";
                    Port.IsEnabled = true;

                    if (_httpServer != null)
                    {
                        _httpServer.IsServerWork = false;
                        _httpServer.StopServer();
                    }
                }
            }
            catch (Exception exception)
            {
                MessageBox.Show(exception.Message, "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private HttpResponse HttpServerOnWebAppEvent(HttpRequest request)
        {
            HttpResponse httpResponse = new HttpResponse();
            int count = 0;

            Logs.Text += $"[{DateTime.Now.ToLongTimeString()}] Request: {request.QueryString}\r\n";
            Logs.Text += "-------------------------------------------------------\r\n";

            try
            {
                switch (request.HttpMethod)
                {
                    case HttpMethod.GET:
                        if (request.Url.IndexOf("remove.html", StringComparison.Ordinal) != -1)
                        {
                            foreach (var parameter in request.Parameters)
                            {
                                if (parameter.Key.Equals("id"))
                                {
                                    Task<bool> result = RemoveEmployee(parameter.Value);

                                    if (result.Result)
                                    {
                                        httpResponse.StatusCode = httpResponse.StatusCodes[200];
                                        httpResponse.ContentType = "text/html; charset=utf-8";

                                        string html = GetWebPageWithAnswerOnRequest("Удаление сотрудника",
                                            "Сотрудник был успешно удален из базы данных!");
                                        httpResponse.Body = html;

                                        httpResponse.Headers.Add("Server", "My http server");
                                        httpResponse.Headers.Add("Content-Length",
                                            Encoding.UTF8.GetBytes(html).LongLength.ToString());
                                        httpResponse.Headers.Add("Connection", "Close");
                                    }
                                    else
                                    {
                                        httpResponse.StatusCode = httpResponse.StatusCodes[204];
                                        httpResponse.ContentType = "text/html; charset=utf-8";

                                        string html = GetWebPageWithAnswerOnRequest("Удаление сотрудника",
                                            "Указанный сотрудник не был найден в базе данных!");
                                        httpResponse.Body = html;

                                        httpResponse.Headers.Add("Server", "My http server");
                                        httpResponse.Headers.Add("Content-Length",
                                            Encoding.UTF8.GetBytes(html).LongLength.ToString());
                                        httpResponse.Headers.Add("Connection", "Close");
                                    }

                                    ++count;
                                }
                            }
                        }
                        else if (request.Url.IndexOf("edit.html", StringComparison.Ordinal) != -1)
                        {
                            foreach (var parameter in request.Parameters)
                            {
                                if (parameter.Key.Equals("id"))
                                {
                                    Task<string> resultTask = GetEditEmployeePage(parameter.Value);

                                    if (!string.IsNullOrEmpty(resultTask.Result))
                                    {
                                        httpResponse.StatusCode = httpResponse.StatusCodes[200];
                                        httpResponse.ContentType = "text/html; charset=utf-8";

                                        httpResponse.Body = resultTask.Result;

                                        httpResponse.Headers.Add("Server", "My http server");
                                        httpResponse.Headers.Add("Content-Length",
                                            Encoding.UTF8.GetBytes(resultTask.Result).LongLength.ToString());
                                        httpResponse.Headers.Add("Connection", "Close");
                                    }
                                    else
                                    {
                                        httpResponse.StatusCode = httpResponse.StatusCodes[204];
                                        httpResponse.ContentType = "text/html; charset=utf-8";

                                        string html = GetWebPageWithAnswerOnRequest("Изменение сотрудника",
                                            "Указанный сотрудник не был найден в базе данных!");
                                        httpResponse.Body = html;

                                        httpResponse.Headers.Add("Server", "My http server");
                                        httpResponse.Headers.Add("Content-Length",
                                            Encoding.UTF8.GetBytes(html).LongLength.ToString());
                                        httpResponse.Headers.Add("Connection", "Close");
                                    }
                                    ++count;
                                }
                            }
                        }
                        else if (request.Url.IndexOf("search.html", StringComparison.Ordinal) != -1 &&
                                 request.Parameters.Count > 0)
                        {
                            foreach (var parameter in request.Parameters)
                            {
                                if (parameter.Key.Equals("employeeName"))
                                {
                                    Task<string> resultTask =
                                        GetEmployeeView("Список искомых сотрудников", true, parameter.Value);

                                    if (!string.IsNullOrEmpty(resultTask.Result))
                                    {
                                        httpResponse.StatusCode = httpResponse.StatusCodes[200];
                                        httpResponse.ContentType = "text/html; charset=utf-8";

                                        httpResponse.Body = resultTask.Result;

                                        httpResponse.Headers.Add("Server", "My http server");
                                        httpResponse.Headers.Add("Content-Length",
                                            Encoding.UTF8.GetBytes(resultTask.Result).LongLength.ToString());
                                        httpResponse.Headers.Add("Connection", "Close");
                                    }
                                    else
                                    {
                                        httpResponse.StatusCode = httpResponse.StatusCodes[204];
                                        httpResponse.ContentType = "text/html; charset=utf-8";

                                        string html = GetWebPageWithAnswerOnRequest("Поиск сотрудников",
                                            "Во время поиска сотрудников произошла ошибка!");
                                        httpResponse.Body = html;

                                        httpResponse.Headers.Add("Server", "My http server");
                                        httpResponse.Headers.Add("Content-Length",
                                            Encoding.UTF8.GetBytes(html).LongLength.ToString());
                                        httpResponse.Headers.Add("Connection", "Close");
                                    }
                                    ++count;
                                }
                            }
                        }
                        else
                        {
                            string fileName = GetFileName(request.Url);
                            fileName = Path.Combine(AppSettings["root_folder"], fileName);

                            if (fileName.Equals(AppSettings["root_folder"]))
                            {
                                Task<string> resultTask =
                                    GetEmployeeView("Список всех сотрудников", false, String.Empty);

                                if (!string.IsNullOrEmpty(resultTask.Result))
                                {
                                    httpResponse.StatusCode = httpResponse.StatusCodes[200];
                                    httpResponse.ContentType = "text/html; charset=utf-8";

                                    httpResponse.Body = resultTask.Result;

                                    httpResponse.Headers.Add("Server", "My http server");
                                    httpResponse.Headers.Add("Content-Length",
                                        Encoding.UTF8.GetBytes(resultTask.Result).LongLength.ToString());
                                    httpResponse.Headers.Add("Connection", "Close");
                                }
                                else
                                {
                                    httpResponse.StatusCode = httpResponse.StatusCodes[404];
                                    httpResponse.ContentType = "text/html; charset=utf-8";

                                    string html = GetError404();
                                    httpResponse.Body = html;

                                    httpResponse.Headers.Add("Server", "My http server");
                                    httpResponse.Headers.Add("Content-Length",
                                        Encoding.UTF8.GetBytes(html).LongLength.ToString());
                                    httpResponse.Headers.Add("Connection", "Close");
                                }

                                ++count;
                            }
                            else
                            {
                                if (File.Exists(fileName))
                                {
                                    httpResponse.StatusCode = httpResponse.StatusCodes[200];
                                    httpResponse.ContentType = "text/html; charset=utf-8";

                                    httpResponse.Body = File.ReadAllText(fileName);

                                    httpResponse.Headers.Add("Server", "My http server");
                                    httpResponse.Headers.Add("Content-Length",
                                        Encoding.UTF8.GetBytes(httpResponse.Body).LongLength.ToString());
                                    httpResponse.Headers.Add("Connection", "Close");
                                }
                                else
                                {
                                    httpResponse.StatusCode = httpResponse.StatusCodes[404];
                                    httpResponse.ContentType = "text/html; charset=utf-8";

                                    string html = GetError404();
                                    httpResponse.Body = html;

                                    httpResponse.Headers.Add("Server", "My http server");
                                    httpResponse.Headers.Add("Content-Length",
                                        Encoding.UTF8.GetBytes(html).LongLength.ToString());
                                    httpResponse.Headers.Add("Connection", "Close");
                                }

                                ++count;
                            }
                        }
                        break;
                    case HttpMethod.POST:
                        if (request.Url.IndexOf("add.html", StringComparison.Ordinal) != -1)
                        {
                            Employee employee = GetEmployee(request.Parameters);

                            Task<bool> resultTask = AddNewEmployee(employee);

                            if (resultTask.Result)
                            {
                                httpResponse.StatusCode = httpResponse.StatusCodes[200];
                                httpResponse.ContentType = "text/html; charset=utf-8";

                                string html = GetWebPageWithAnswerOnRequest("Добавление сотрудника",
                                    "Новый сотрудник был успешно добавлен в базу данных!");
                                httpResponse.Body = html;

                                httpResponse.Headers.Add("Server", "My http server");
                                httpResponse.Headers.Add("Content-Length",
                                    Encoding.UTF8.GetBytes(html).LongLength.ToString());
                                httpResponse.Headers.Add("Connection", "Close");
                            }
                            else
                            {
                                httpResponse.StatusCode = httpResponse.StatusCodes[204];
                                httpResponse.ContentType = "text/html; charset=utf-8";

                                string html = GetWebPageWithAnswerOnRequest("Добавление сотрудника",
                                    "Сотрудник не был успешно добавлен в базу данных!");
                                httpResponse.Body = html;

                                httpResponse.Headers.Add("Server", "My http server");
                                httpResponse.Headers.Add("Content-Length",
                                    Encoding.UTF8.GetBytes(html).LongLength.ToString());
                                httpResponse.Headers.Add("Connection", "Close");
                            }
                            ++count;
                        }
                        else if (request.Url.IndexOf("edit.html", StringComparison.Ordinal) != -1)
                        {
                            Employee employee = GetEmployee(request.Parameters);

                            Task<bool> resultTask = EditEmployee(employee);

                            if (resultTask.Result)
                            {
                                httpResponse.StatusCode = httpResponse.StatusCodes[200];
                                httpResponse.ContentType = "text/html; charset=utf-8";

                                string html = GetWebPageWithAnswerOnRequest("Изменение данных сотрудника",
                                    "Данные сотрудника были успешно изменены!");
                                httpResponse.Body = html;

                                httpResponse.Headers.Add("Server", "My http server");
                                httpResponse.Headers.Add("Content-Length",
                                    Encoding.UTF8.GetBytes(html).LongLength.ToString());
                                httpResponse.Headers.Add("Connection", "Close");
                            }
                            else
                            {
                                httpResponse.StatusCode = httpResponse.StatusCodes[204];
                                httpResponse.ContentType = "text/html; charset=utf-8";

                                string html = GetWebPageWithAnswerOnRequest("Изменение данных сотрудника",
                                    "Данные сотрудника не были успешно изменены!");
                                httpResponse.Body = html;

                                httpResponse.Headers.Add("Server", "My http server");
                                httpResponse.Headers.Add("Content-Length",
                                    Encoding.UTF8.GetBytes(html).LongLength.ToString());
                                httpResponse.Headers.Add("Connection", "Close");
                            }
                            ++count;
                        }
                        break;
                }

                if (count == 0)
                {
                    httpResponse.StatusCode = httpResponse.StatusCodes[400];
                    httpResponse.ContentType = "text/html; charset=utf-8";

                    string html =
                        GetWebPageWithAnswerOnRequest("Ошибка запроса", $"Error {httpResponse.StatusCodes[400]}!");
                    httpResponse.Body = html;

                    httpResponse.Headers.Add("Server", "My http server");
                    httpResponse.Headers.Add("Content-Length", Encoding.UTF8.GetBytes(html).LongLength.ToString());
                    httpResponse.Headers.Add("Connection", "Close");
                }
            }
            catch (Exception exception)
            {
                MessageBox.Show(exception.Message, "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }

            return httpResponse;
        }
        
        private async Task<bool> AddNewEmployee(Employee employee)
        {
            bool result = false;

            try
            {
                if (employee != null)
                {
                    using (EmployeeContext context = new EmployeeContext())
                    {
                        Employee employee2 = context.Employees.FirstOrDefault(
                            t => t.Name.Equals(employee.Name) &&
                                 t.Age == employee.Age &&
                                 t.Post.Equals(employee.Post) &&
                                 t.Salary == employee.Salary);

                        if (employee2 == null)
                        {
                            context.Employees.Add(employee);
                            context.SaveChanges();
                            result = true;
                        }
                    }
                }
            }
            catch (Exception)
            {
                result = false;
            }

            return result;
        }
        
        private async Task<bool> EditEmployee(Employee newEmployee)
        {
            bool result = false;

            try
            {
                if (newEmployee != null)
                {
                    using (EmployeeContext context = new EmployeeContext())
                    {
                        Employee employee = context.Employees.FirstOrDefault(t => t.Id == newEmployee.Id);

                        if (employee != null)
                        {
                            employee.Id = newEmployee.Id;
                            employee.Name = newEmployee.Name;
                            employee.Age = newEmployee.Age;
                            employee.Post = newEmployee.Post;
                            employee.Salary = newEmployee.Salary;

                            context.SaveChanges();

                            result = true;
                        }
                    }
                }
            }
            catch (Exception)
            {
                result = false;
            }

            return result;
        }

        private async Task<bool> RemoveEmployee(string employeeId)
        {
            bool result = false;

            try
            {
                using (EmployeeContext context = new EmployeeContext())
                {
                    int id = Convert.ToInt32(employeeId);

                    Employee employee = context.Employees.FirstOrDefault(t => t.Id == id);

                    if (employee != null)
                    {
                        context.Employees.Remove(employee);
                        context.SaveChanges();

                        Logs.Text += $"[{DateTime.Now.ToLongTimeString()}] Сотрудник [{employee}] был успешно удален из базы данных!\r\n";

                        result = true;
                    }
                }
            }
            catch (Exception)
            {
                result = false;
            }

            return result;
        }

        private Employee GetEmployee(Dictionary<string, string> requestParameters)
        {
            Employee employee = new Employee();

            int count = 0;
            foreach (var parameter in requestParameters)
            {
                if (parameter.Key.Equals("employeeId"))
                {
                    employee.Id = Convert.ToInt32(parameter.Value);
                    ++count;
                }
                else if (parameter.Key.Equals("employeeName"))
                {
                    employee.Name = parameter.Value;
                    ++count;
                }
                else if (parameter.Key.Equals("employeeAge"))
                {
                    employee.Age = Convert.ToInt32(parameter.Value);
                    ++count;
                }
                else if (parameter.Key.Equals("employeePost"))
                {
                    employee.Post = parameter.Value;
                    ++count;
                }
                else if (parameter.Key.Equals("employeeSalary"))
                {
                    employee.Salary = Convert.ToDecimal(parameter.Value);
                    ++count;
                }
            }

            if (count != requestParameters.Count)
            {
                employee = null;
            }

            return employee;
        }
        
        private async Task<string> GetEmployeeView(string title, bool isSearchByName, string name)
        {
            string html;

            try
            {
                using (EmployeeContext context = new EmployeeContext())
                {
                    List<Employee> employees = context.Employees.ToList();

                    StringBuilder builder = new StringBuilder();
                    int count = 0;
                    foreach (var employee in employees)
                    {
                        if (isSearchByName)
                        {
                            if (employee.Name.IndexOf(name, StringComparison.OrdinalIgnoreCase) != -1)
                            {
                                GetEmployeeHtmlCode(employee, builder);
                                ++count;
                            }
                        }
                        else
                        {
                            GetEmployeeHtmlCode(employee, builder);
                            ++count;
                        }
                    }

                    html = File.ReadAllText(Path.Combine(AppSettings["template_folder"], AppSettings["view_page"]));

                    html = html.Replace("{title}", title);
                    html = html.Replace("{text}", isSearchByName == false ? "Список сотрудников" : $"По указанным параметрам было найденно {count} сотрудников");
                    html = html.Replace("{EMPLOYEES}", builder.ToString());
                }
            }
            catch (Exception )
            {
                html = String.Empty;
            }

            return html;
        }

        private void GetEmployeeHtmlCode(Employee employee, StringBuilder builder, char sym = '\"')
        {
            builder.Append($@"<tr>
                                    <td>{employee.Id}</td>
                                    <td>{employee.Name}</td>
                                    <td>{employee.Age}</td>
                                    <td>{employee.Post}</td>
                                    <td>{employee.Salary}</td>
                                    <td>
                                        <form method={sym}GET{sym} action={sym}remove.html{sym}>
                                            <input type={sym}hidden{sym} name={sym}id{sym} value={sym}{employee.Id}{sym} />     
                                            <input type={sym}submit{sym} value={sym}Удалить{sym} class={sym}button2{sym} />
        
                                        </form>
                                    </td>
                                    <td>
                                        <form method={sym}GET{sym} action={sym}edit.html{sym}>
                                            <input type={sym}hidden{sym} name={sym}id{sym} value={sym}{employee.Id}{sym} />
                                            <input type={sym}submit{sym} value={sym}Изменить{sym} class={sym}button3{sym}/>
                                        </form>
                                    </td>
                                   </tr>");
        }
        
        private async Task<string> GetEditEmployeePage(string employeeId)
        {
            string html = string.Empty;

            try
            {
                using (EmployeeContext context = new EmployeeContext())
                {
                    int id = Convert.ToInt32(employeeId);

                    Employee employee = context.Employees.FirstOrDefault(t => t.Id == id);

                    if (employee != null)
                    {
                        html = File.ReadAllText(Path.Combine(AppSettings["template_folder"], AppSettings["edit_form"]));

                        html = html.Replace("{ID}", employee.Id.ToString());
                        html = html.Replace("{NAME}", employee.Name);
                        html = html.Replace("{AGE}", employee.Age.ToString());
                        html = html.Replace("{POST}", employee.Post);
                        html = html.Replace("{SALARY}", employee.Salary.ToString().Substring(0, employee.Salary.ToString().IndexOf(",")));
                    }
                }
            }
            catch (Exception)
            {
               html = String.Empty;
            }

            return html;
        }

        private string GetWebPageWithAnswerOnRequest(string title, string text)
        {
            char sym = '\"';
            char sym2 = '{';
            char sym3 = '}';
            string html = $@"<!DOCTYPE html>
<html>
    <head>
        <meta charset={sym}utf-8{sym}/>
                <title>{title}</title>
  
                <script>
                function doRedirect() {sym2}
                atTime={sym}2500{sym};
                toUrl={sym}/{sym};

                setTimeout({sym}location.href=toUrl;{sym}, atTime);
            {sym3}
            </script>
    </head>
    <body onload={sym}doRedirect(){sym} style={sym}text-align: center; margin-top: 10 %{sym}> 
        <h1 style={sym}text-align: center{sym}>{text}</h1>
    </body>
</html>";

            return html;
        }

        private string GetFileName(string url)
        {
            string nameOfRequestedFile = string.Empty;

            try
            {
                string parsePath = url.Substring(url.IndexOf('/') + 1);

                foreach (var path in parsePath.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    nameOfRequestedFile = Path.Combine(nameOfRequestedFile, path);
                }
            }
            catch (Exception exception)
            {
                MessageBox.Show(exception.Message, "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);

                nameOfRequestedFile = string.Empty;
            }

            return nameOfRequestedFile;
        }

        private string GetError404()
        {
            char sym = '\"';
            string html = $@"<!doctype html>
<html>
    <head>
        <title>Error 404</title>
        <meta http-equiv={sym}content-type{sym} content={sym}text/html; charset=utf-8{sym} />
        <style>
            .button {"{"}
                background-color: #4CAF50;
                border: none;
                color: white;
                text-align: center;
                padding: 15px 32px;                
                text-decoration: none;
                display: inline-block;
                font-size: 16px;
                margin-left: auto;
                margin-right: auto;				
            {"}"}
        </style>
    </head>
    <body>
        <div style={sym}text-align: center;margin-top: 10%{sym}>
            <img style={sym}vertical-align: middle;{sym} src={
                    sym
                }http://sas-network.org/Assets/Images/404.png{sym} alt={sym}Eror 404{
                    sym
                } width={sym}700{
                    sym
                } height={sym}420{sym}/>
                <br>
                <button onclick={sym}window.location.href = '/'{sym} class={sym}button{sym}>Главная страница</button>
        </div>
    </body>
</html>";
            return html;
        }

        private void ClearButton_OnClick(object sender, RoutedEventArgs e)
        {
            Logs.Text = string.Empty;
        }
    }
}