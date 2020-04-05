<Query Kind="Program">
  <Reference>&lt;RuntimeDirectory&gt;\System.DirectoryServices.AccountManagement.dll</Reference>
  <Reference>&lt;RuntimeDirectory&gt;\System.DirectoryServices.dll</Reference>
  <Reference>&lt;RuntimeDirectory&gt;\System.DirectoryServices.Protocols.dll</Reference>
  <Reference>&lt;RuntimeDirectory&gt;\System.Configuration.dll</Reference>
  <NuGetReference>System.DirectoryServices.AccountManagement</NuGetReference>
  <Namespace>System.DirectoryServices.AccountManagement</Namespace>
</Query>

void Main()
{

}


string Domain;
string DefaultOU;
string ServiceUser;
string ServicePassword;

public UserPrincipal CreateNewUser(string sUserName, string sPassword)
{
	// first check that the user doesn't exist
	if (GetUser(sUserName) == null)
	{
		PrincipalContext oPrincipalContext = GetPrincipalContext();

		UserPrincipal oUserPrincipal = new UserPrincipal(oPrincipalContext);
		oUserPrincipal.Name = sUserName;
		oUserPrincipal.SetPassword(sPassword);
		//User Log on Name
		//oUserPrincipal.UserPrincipalName = sUserName;
		oUserPrincipal.Save();

		return oUserPrincipal;
	}

	// if it already exists, return the old user
	return GetUser(sUserName);
}

/// <summary>
/// Gets a list of the users group memberships
/// </summary>
/// <param name="sUserName">The user you want to get the group memberships</param>
/// <returns>Returns an arraylist of group memberships</returns>
public ArrayList GetUserGroups(string sUserName)
{
	ArrayList myItems = new ArrayList();
	UserPrincipal oUserPrincipal = GetUser(sUserName);

	PrincipalSearchResult<Principal> oPrincipalSearchResult = oUserPrincipal.GetGroups();

	foreach (Principal oResult in oPrincipalSearchResult)
	{
		myItems.Add(oResult.Name);
	}
	return myItems;
}



/// <summary>
/// Gets a certain user on Active Directory
/// </summary>
/// <param name="sUserName">The username to get</param>
/// <returns>Returns the UserPrincipal Object</returns>
public UserPrincipal GetUser(string sUserName)
{
	PrincipalContext oPrincipalContext = GetPrincipalContext();

	UserPrincipal oUserPrincipal = UserPrincipal.FindByIdentity(oPrincipalContext, sUserName);
	return oUserPrincipal;
}


/// <summary>
/// Gets the base principal context
/// </summary>
/// <returns>Retruns the PrincipalContext object</returns>
public PrincipalContext GetPrincipalContext()
{
	PrincipalContext oPrincipalContext = new PrincipalContext(ContextType.Domain, Domain, DefaultOU, ContextOptions.SimpleBind, ServiceUser, ServicePassword);
	return oPrincipalContext;
}