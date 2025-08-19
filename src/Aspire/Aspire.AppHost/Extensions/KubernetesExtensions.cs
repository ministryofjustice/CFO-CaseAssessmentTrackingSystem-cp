using Aspire.Hosting.Kubernetes.Resources;

namespace Aspire.Hosting.Kubernetes;

public static class KubernetesExtensions
{
    public static void WithIngress(this KubernetesResource service, int port, string k8s_namespace = "default")
    {
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
        var ingress = new Ingress
        {
            Metadata = new ObjectMetaV1
            {
                Name = $"{service.Name}-ingress",
                Annotations =
                {
                    { "external-dns.alpha.kubernetes.io/set-identifier", "cats-ingress-cfocats-dev-green" },
                    { "external-dns.alpha.kubernetes.io/aws-weight", "100" },
                    // https://learn.microsoft.com/en-us/aspnet/core/blazor/host-and-deploy/server#kubernetes
                    { "nginx.ingress.kubernetes.io/affinity", "cookie" },
                    { "nginx.ingress.kubernetes.io/session-cookie-name", "affinity" },
                    { "nginx.ingress.kubernetes.io/session-cookie-expires", "14400" },
                    { "nginx.ingress.kubernetes.io/session-cookie-max-age", "14400" },
                }
            },
            Spec = new IngressSpecV1
            {
                IngressClassName = "default",
                Tls =
                {
                    new IngressTLSV1()
                    {
                        Hosts = 
                        { 
                            $"{k8s_namespace}.live.cloud-platform.service.justice.gov.uk" 
                        }
                    }
                },
                Rules =
                {
                    new IngressRuleV1
                    {
                        Host = $"{k8s_namespace}.live.cloud-platform.service.justice.gov.uk" ,
                        Http = new HttpIngressRuleValueV1
                        {
                            Paths =
                            {
                                new HttpIngressPathV1
                                {
                                    Path = "/",
                                    PathType = "ImplementationSpecific",
                                    Backend = new IngressBackendV1
                                    {
                                        Service = new IngressServiceBackendV1
                                        {
                                            Name = $"{service.Name}-service",
                                            Port = new ServiceBackendPortV1 { Number = port }
                                        },
                                        Resource = null
                                    }
                                }
                            }
                        }
                    }
                }
            }
        };

        service.AdditionalResources.Add(ingress);
    }
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.

}