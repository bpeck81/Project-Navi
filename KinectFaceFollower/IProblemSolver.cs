using System.ServiceModel;

[ServiceContract(Namespace = "urn:ps")]
interface IProblemSolver
{
    [OperationContract]
    int AddNumbers(int a, int b);
}

interface IProblemSolverChannel : IProblemSolver, IClientChannel { }


[ServiceContract(Namespace = "urn:ps")]
interface IDoorbell
{
    [OperationContract]
    bool IsAllowedIn(string data);
}

interface IDoorbellChannel : IDoorbell, IClientChannel { }