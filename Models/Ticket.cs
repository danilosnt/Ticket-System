using System;

namespace Ticket_System.Models;

public enum TicketStatus
{
    Aberto,
    EmAndamento,
    Finalizado,
    Cancelado
}

public class Ticket
{
    public Guid Id { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public string Sector { get; private set; } = string.Empty;
    public string Priority { get; private set; } = string.Empty;
    public TicketStatus Status { get; private set; }
    public string? ImagePath { get; private set; }

    public DateTime CreatedAt { get; private set; }
    public DateTime? StartedAt { get; private set; }
    public DateTime? FinishedAt { get; private set; }
    public string? ResolutionNotes { get; private set; }

    // User tracking
    public Guid CreatedByUserId { get; private set; }
    public Guid? StartedByUserId { get; private set; }
    public Guid? FinishedByUserId { get; private set; }
    public Guid? CancelledByUserId { get; private set; }

    protected Ticket() { }

    public Ticket(string title, string description, string sector, string priority, Guid createdByUserId, string? imagePath = null)
    {
        Id = Guid.NewGuid();
        Title = title;
        Description = description;
        Sector = sector;
        Priority = priority;
        Status = TicketStatus.Aberto;
        CreatedAt = DateTime.UtcNow;
        CreatedByUserId = createdByUserId;
        ImagePath = imagePath;
    }

    public void StartAttendance(DateTime currentTime, Guid startedByUserId)
    {
        if (Status == TicketStatus.Finalizado || Status == TicketStatus.Cancelado)
        {
            throw new InvalidOperationException($"Não é possível iniciar um chamado já {Status}.");
        }

        if (Status == TicketStatus.EmAndamento)
        {
            throw new InvalidOperationException("O chamado já está em atendimento.");
        }

        Status = TicketStatus.EmAndamento;
        StartedAt = currentTime;
        StartedByUserId = startedByUserId;
    }

    public void FinishAttendance(string solution, DateTime currentTime, Guid finishedByUserId)
    {
        if (Status != TicketStatus.EmAndamento)
        {
            throw new InvalidOperationException("Apenas chamados em andamento podem ser finalizados.");
        }

        if (string.IsNullOrWhiteSpace(solution))
        {
            throw new ArgumentException("Uma solução detalhada deve ser informada.");
        }

        Status = TicketStatus.Finalizado;
        FinishedAt = currentTime;
        ResolutionNotes = solution;
        FinishedByUserId = finishedByUserId;
    }

    public void CancelTicket(string reason, DateTime currentTime, Guid cancelledByUserId)
    {
        if (Status == TicketStatus.Finalizado)
        {
            throw new InvalidOperationException("Não é possível cancelar um chamado já finalizado.");
        }

        if (Status == TicketStatus.Cancelado)
        {
            throw new InvalidOperationException("O chamado já está cancelado.");
        }

        Status = TicketStatus.Cancelado;
        FinishedAt = currentTime;
        ResolutionNotes = reason;
        CancelledByUserId = cancelledByUserId;
    }
}