namespace Robot.Domain.Commands;

public class CommandValidation
{
    public bool ValidateMode(string mode) => mode == "IDLE" || mode == "MANUAL" || mode == "PAUSED";
}
