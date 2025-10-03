namespace NextPeople.Services;

public class TicketStateService
{
    // --- ESTADO DA APLICAÇÃO ---
    public List<ServiceStage> Stages { get; private set; } = new();
    public List<Ticket> WaitingQueue { get; private set; } = new();
    public List<CalledTicketInfo> History { get; private set; } = new();
    public bool AutoForwardEnabled { get; private set; } = false;

    private int commonTicketCounter = 1;
    private int priorityTicketCounter = 1;

    public event Action? OnChange;

    private void NotifyStateChanged() => OnChange?.Invoke();

    public void CallNextInStage(ServiceStage stage, Guid workstationId)
    {
        var workstation = stage.Workstations.FirstOrDefault(w => w.Id == workstationId);
        if (workstation == null || workstation.CurrentTicket != null) return;

        var nextTicket = stage.WaitingTickets.OrderByDescending(t => t.IsPriority).ThenBy(t => t.Timestamp).FirstOrDefault();
        if (nextTicket != null)
        {
            stage.WaitingTickets.Remove(nextTicket);
            workstation.CurrentTicket = nextTicket;

            var calledInfo = new CalledTicketInfo
            {
                TicketNumber = nextTicket.Number,
                StageName = stage.Name,
                WorkstationName = $"{stage.WorkstationTypeName} {workstation.Name}",
                Timestamp = DateTime.Now
            };

            History.Insert(0, calledInfo);
            if (History.Count > 50) History.RemoveAt(History.Count - 1);

            NotifyStateChanged();
        }
    }

    public void GenerateTicket(bool isPriority)
    {
        Ticket newTicket;
        if (isPriority)
        {
            newTicket = new Ticket { Number = $"P{priorityTicketCounter:000}", IsPriority = true, Timestamp = DateTime.Now };
            priorityTicketCounter++;
        }
        else
        {
            newTicket = new Ticket { Number = $"C{commonTicketCounter:000}", IsPriority = false, Timestamp = DateTime.Now };
            commonTicketCounter++;
        }

        if (AutoForwardEnabled && Stages.Any())
        {
            Stages.First().WaitingTickets.Add(newTicket);
        }
        else
        {
            WaitingQueue.Add(newTicket);
        }
        NotifyStateChanged();
    }

    public void MoveTicket(Ticket ticket, ServiceStage? fromStage, ServiceStage? toStage)
    {
        if (fromStage == null) WaitingQueue.Remove(ticket);
        else fromStage.WaitingTickets.Remove(ticket);

        if (toStage == null) WaitingQueue.Add(ticket);
        else toStage.WaitingTickets.Add(ticket);

        NotifyStateChanged();
    }

    public void MoveTicketFromWorkstation(Workstation fromWorkstation, Ticket ticket, ServiceStage toStage)
    {
        fromWorkstation.CurrentTicket = null;
        toStage.WaitingTickets.Add(ticket);
        NotifyStateChanged();
    }

    public void FinishTicket(Workstation workstation)
    {
        workstation.CurrentTicket = null;
        NotifyStateChanged();
    }

    public void AddStage(string name, string workstationType)
    {
        if (!string.IsNullOrWhiteSpace(name))
        {
            Stages.Add(new ServiceStage { Name = name, WorkstationTypeName = workstationType });
            NotifyStateChanged();
        }
    }

    public void RemoveStage(ServiceStage stage)
    {
        WaitingQueue.AddRange(stage.WaitingTickets);
        foreach (var ws in stage.Workstations)
        {
            if (ws.CurrentTicket != null) WaitingQueue.Add(ws.CurrentTicket);
        }
        Stages.Remove(stage);
        NotifyStateChanged();
    }

    public void IncrementWorkstation(ServiceStage stage)
    {
        var maxNumber = stage.Workstations.Select(w => int.TryParse(w.Name, out var num) ? num : 0).DefaultIfEmpty(0).Max();
        stage.Workstations.Add(new Workstation { Name = (maxNumber + 1).ToString() });
        NotifyStateChanged();
    }

    public void DecrementWorkstation(ServiceStage stage)
    {
        if (!stage.Workstations.Any()) return;
        var lastWorkstation = stage.Workstations.OrderByDescending(w => int.TryParse(w.Name, out var num) ? num : 0).First();
        if (lastWorkstation.CurrentTicket != null) stage.WaitingTickets.Add(lastWorkstation.CurrentTicket);
        stage.Workstations.Remove(lastWorkstation);
        NotifyStateChanged();
    }

    public void ToggleAutoForward()
    {
        AutoForwardEnabled = !AutoForwardEnabled;
        NotifyStateChanged();
    }

    // --- CLASSES DE MODELO ---
    public class Ticket { public Guid Id { get; set; } = Guid.NewGuid(); public string Number { get; set; } = ""; public bool IsPriority { get; set; } public DateTime Timestamp { get; set; } }
    public class Workstation { public Guid Id { get; set; } = Guid.NewGuid(); public string Name { get; set; } = ""; public Ticket? CurrentTicket { get; set; } }
    public class ServiceStage { public Guid Id { get; set; } = Guid.NewGuid(); public string Name { get; set; } = ""; public string WorkstationTypeName { get; set; } = "Guichê"; public List<Workstation> Workstations { get; set; } = new(); public List<Ticket> WaitingTickets { get; set; } = new(); }
    public class CalledTicketInfo { public string TicketNumber { get; set; } = string.Empty; public string StageName { get; set; } = string.Empty; public string WorkstationName { get; set; } = string.Empty; public DateTime Timestamp { get; set; } }
}

