namespace DicingBlade.Classes.Processes
{
    public record ProcessMessage(MessageType MessageType, string Message):IProcessNotify;
}
