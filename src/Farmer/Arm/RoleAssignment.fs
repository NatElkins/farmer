[<AutoOpen>]
module Farmer.Arm.RoleAssignment

open Farmer
open Farmer.CoreTypes

let roleAssignments = ResourceType ("Microsoft.Authorization/roleAssignments", "2020-04-01-preview")

[<RequireQualifiedAccess>]
type PrincipalType =
    | User
    | Group
    | ServicePrincipal
    | Unknown
    | DirectoryRoleTemplate
    | ForeignGroup
    | Application
    | MSI
    | DirectoryObjectOrGroup
    | Everyone
    member this.ArmValue =
        match this with
        | User -> "User"
        | Group -> "Group"
        | ServicePrincipal -> "ServicePrincipal"
        | Unknown -> "Unknown"
        | DirectoryRoleTemplate -> "DirectoryRoleTemplate"
        | ForeignGroup -> "ForeignGroup"
        | Application -> "Application"
        | MSI -> "MSI"
        | DirectoryObjectOrGroup -> "DirectoryObjectOrGroup"
        | Everyone -> "Everyone"

/// The scope that an assignment applies to - either a specific resource, or the entire resource group.
type AssignmentScope = ResourceGroup | SpecificResource of ResourceId

type RoleAssignment =
    { /// It's recommended to use a deterministic GUID for the role name.
      Name : ResourceName
      /// The role to assign, such as Roles.Contributor
      RoleDefinitionId : RoleId
      /// The principal ID of the user or service identity that should be granted this role.
      PrincipalId : PrincipalId
      /// The type of principal being assigned - should be set to ServicePrincipal for managed identities to avoid
      /// the role assignment being created before Active Directory can replicate the principal.
      PrincipalType : PrincipalType
      /// Resource this role applies to.
      Scope : AssignmentScope }
    
    member private this.Dependencies = [
        match this.Scope with
        | SpecificResource resourceId -> resourceId
        | ResourceGroup -> ()
    ]
    interface IArmResource with
        member this.ResourceName = this.Name
        member this.JsonModel =
            {| roleAssignments.Create(this.Name, dependsOn = this.Dependencies) with
                properties =
                    {| roleDefinitionId = this.RoleDefinitionId.ArmValue.Eval()
                       principalId = this.PrincipalId.ArmExpression.Eval()
                       scope =
                        match this.Scope with
                        | SpecificResource resourceId -> resourceId.Eval()
                        | ResourceGroup -> null
                       principalType = this.PrincipalType.ArmValue
                    |}
            |}:> _
