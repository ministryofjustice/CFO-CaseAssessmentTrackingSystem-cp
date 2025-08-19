using Aspire.Hosting.Kubernetes;

var builder = DistributedApplication.CreateBuilder(args);

var publishing = builder.ExecutionContext.IsPublishMode;

var sqlPassword = builder.AddParameter("sqlPassword", secret: true);
var rabbitUser = builder.AddParameter("rabbitUser", secret: true);
var rabbitPassword = builder.AddParameter("rabbitPassword", secret: true);

var k8s = builder.AddKubernetesEnvironment("k8s").WithProperties(configure: resource =>
{
    resource.DefaultImagePullPolicy = "Always";
});

#pragma warning disable ASPIREPROXYENDPOINTS001
var sql = builder.AddSqlServer("sql", sqlPassword, 1433)
    .WithDataVolume("cats-aspire-data")
    .WithLifetime(ContainerLifetime.Persistent)
    .WithEndpointProxySupport(false)
    .ExcludeFromManifest();
#pragma warning restore ASPIREPROXYENDPOINTS001

var catsDb = sql.AddDatabase("CatsDb");

var rabbit = builder.AddRabbitMQ("rabbit",
        userName: rabbitUser,
        password: rabbitPassword,
        port: 5672)
    .WithDataVolume("cats-aspire-rabbit")
    .WithManagementPlugin(port: 15672)
    .WithLifetime(ContainerLifetime.Persistent)
    .ExcludeFromManifest();

var cats = builder.AddProject<Projects.Server_UI>("cats", configure: project =>
    {
        // Exclude launchSettings on publish
        project.ExcludeLaunchProfile = publishing;
    });

if (publishing is false)
{
    // Attach local dependencies
    cats.WithReference(catsDb)
        .WaitFor(sql)
        .WithReference(rabbit);
}
else
{
    const int defaultPort = 8080;
    const string k8s_namespace = "cfocats-dev";

    cats.WithHttpEndpoint(port: defaultPort, targetPort: defaultPort)
        .WithReplicas(3)
        .PublishAsKubernetesService(configure: project =>
        {
            // Attach service account
            project.Workload!.PodTemplate.Spec.ServiceAccountName = k8s_namespace;

            // Add ingress
            project.WithIngress(defaultPort, k8s_namespace);

            project.Workload.PodTemplate.Spec.Containers.ForEach(container =>
            {
                container.EnvFrom.AddRange
                (
                    new()
                    {
                        SecretRef = new() { Name = "rds-mssql-instance-output" }
                    },
                    new()
                    {
                        SecretRef = new() { Name = "s3-bucket-output" }
                    },
                    new()
                    {
                        SecretRef = new() { Name = "ec-cluster-output" }
                    }
                );
            });

        });
}

builder.Build().Run();