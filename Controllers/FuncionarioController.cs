using Azure.Data.Tables;
using Microsoft.AspNetCore.Mvc;
using TrilhaNetAzureDesafio.Context;
using TrilhaNetAzureDesafio.Models;

namespace TrilhaNetAzureDesafio.Controllers;

[ApiController]
[Route("[controller]")]
public class FuncionarioController : ControllerBase
{
    private readonly RHContext _context;
    private readonly string _connectionString;
    private readonly string _tableName;

    public FuncionarioController(RHContext context, IConfiguration configuration)
    {
        _context = context;
        _connectionString = configuration.GetValue<string>("ConnectionStrings:SAConnectionString");
        _tableName = configuration.GetValue<string>("ConnectionStrings:AzureTableName");
    }

    private TableClient GetTableClient()
    {
        var serviceClient = new TableServiceClient(_connectionString);
        var tableClient = serviceClient.GetTableClient(_tableName);

        tableClient.CreateIfNotExists();
        return tableClient;
    }

    [HttpGet("{id}")]
    public IActionResult ObterPorId(int id)
    {
        var funcionario = _context.Funcionarios.Find(id);

        if (funcionario == null)
            return NotFound();

        return Ok(funcionario);
    }

    [HttpPost]
    public IActionResult Criar(Funcionario funcionario)
    {
        // Validação: campos obrigatórios
        if (string.IsNullOrWhiteSpace(funcionario.Nome))
            return BadRequest("O campo Nome é obrigatório.");

        if (string.IsNullOrWhiteSpace(funcionario.Departamento))
            return BadRequest("O campo Departamento é obrigatório.");

        // Remove o ID se fornecido - o banco gera automaticamente
        funcionario.Id = 0;

        _context.Funcionarios.Add(funcionario);
        _context.SaveChanges();

        var tableClient = GetTableClient();
        var funcionarioLog = new FuncionarioLog(funcionario, TipoAcao.Inclusao, funcionario.Departamento, Guid.NewGuid().ToString());

        tableClient.UpsertEntity(funcionarioLog);

        return CreatedAtAction(nameof(ObterPorId), new { id = funcionario.Id }, funcionario);
    }

    [HttpPut("{id}")]
    public IActionResult Atualizar(int id, Funcionario funcionario)
    {
        // Validação: campos obrigatórios
        if (string.IsNullOrWhiteSpace(funcionario.Nome))
            return BadRequest("O campo Nome é obrigatório.");

        if (string.IsNullOrWhiteSpace(funcionario.Departamento))
            return BadRequest("O campo Departamento é obrigatório.");

        var funcionarioBanco = _context.Funcionarios.Find(id);

        if (funcionarioBanco == null)
            return NotFound();

        funcionarioBanco.Nome = funcionario.Nome;
        funcionarioBanco.Endereco = funcionario.Endereco;
        funcionarioBanco.Ramal = funcionario.Ramal;
        funcionarioBanco.EmailProfissional = funcionario.EmailProfissional;
        funcionarioBanco.Departamento = funcionario.Departamento;
        funcionarioBanco.Salario = funcionario.Salario;
        funcionarioBanco.DataAdmissao = funcionario.DataAdmissao;

        _context.Funcionarios.Update(funcionarioBanco);
        _context.SaveChanges();

        var tableClient = GetTableClient();
        var funcionarioLog = new FuncionarioLog(funcionarioBanco, TipoAcao.Atualizacao, funcionarioBanco.Departamento, Guid.NewGuid().ToString());

        tableClient.UpsertEntity(funcionarioLog);

        return Ok();
    }

    [HttpDelete("{id}")]
    public IActionResult Deletar(int id)
    {
        var funcionarioBanco = _context.Funcionarios.Find(id);

        if (funcionarioBanco == null)
            return NotFound();

        _context.Funcionarios.Remove(funcionarioBanco);
        _context.SaveChanges();

        var tableClient = GetTableClient();
        var funcionarioLog = new FuncionarioLog(funcionarioBanco, TipoAcao.Remocao, funcionarioBanco.Departamento, Guid.NewGuid().ToString());

        tableClient.UpsertEntity(funcionarioLog);

        return NoContent();
    }

    [HttpGet("Logs")]
    public IActionResult ObterLogs()
    {
        var tableClient = GetTableClient();
        var logs = tableClient.Query<FuncionarioLog>().ToList();

        return Ok(logs);
    }

    [HttpGet("Logs/{departamento}")]
    public IActionResult ObterLogsPorDepartamento(string departamento)
    {
        var tableClient = GetTableClient();
        var logs = tableClient.Query<FuncionarioLog>(log => log.PartitionKey == departamento).ToList();

        return Ok(logs);
    }

    [HttpGet("TestConnection")]
    public IActionResult TestConnection()
    {
        try
        {
            // Test SQL Database connection
            var canConnectToDb = _context.Database.CanConnect();

            // Test Table Storage connection string (without exposing full value)
            var hasTableConnection = !string.IsNullOrEmpty(_connectionString);
            var hasTableName = !string.IsNullOrEmpty(_tableName);

            return Ok(new
            {
                SqlDatabase = new
                {
                    Connected = canConnectToDb,
                    Message = canConnectToDb ? "SQL connection successful" : "SQL connection failed"
                },
                TableStorage = new
                {
                    HasConnectionString = hasTableConnection,
                    HasTableName = hasTableName,
                    TableName = _tableName,
                    ConnectionStringPrefix = hasTableConnection ? _connectionString.Substring(0, Math.Min(30, _connectionString.Length)) + "..." : "Not configured"
                }
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                Error = ex.Message,
                InnerError = ex.InnerException?.Message,
                StackTrace = ex.StackTrace
            });
        }
    }
}
