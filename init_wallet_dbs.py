import json
import sys
import os

def read_appsettings(filename):
    import io
    with io.open(filename, "r", encoding="utf-8-sig") as json_file:  
        return json.load(json_file)

def get_mysql_details(data):
    mysql = data["Wallet"]["MySql"]
    return (mysql["Host"], mysql["User"], mysql["Password"])

def get_db_names(data):
    db_names = data["Wallet"]["DbNames"]
    chain_assets = data["Wallet"]["ChainAssetSettings"]
    chain_db_names = []
    fiat_db_names = []
    for key in db_names.keys():
        if key in chain_assets.keys():
            chain_db_names.append(db_names[key])
        else:
            fiat_db_names.append(db_names[key])
    return (chain_db_names, fiat_db_names)

def init_db(project, startup_project, context, host, db_name, user, password):
    print(":: create db '%s'.." % db_name) 
    os.environ["CONNECTION_STRING"] = "host=%s;database=%s;uid=%s;password=%s;" % (host, db_name, user, password)
    print("   > %s" % os.environ["CONNECTION_STRING"])
    cmd = "/opt/dotnet/dotnet-ef database update --project %s --startup-project %s --context %s" % (project, startup_project, context)
    print("   > %s" % cmd)
    res = os.system(cmd)
    if res != 0:
        sys.exit(res)


def init_dbs(project, startup_project, host, user, password, chain_db_names, fiat_db_names):
    #os.chdir(directory)
    os.environ["DB_TYPE"] = "mysql"
    for db_name in chain_db_names:
        init_db(project, startup_project, "WalletContext", host, db_name, user, password)
    for db_name in fiat_db_names:
        init_db(project, startup_project, "FiatWalletContext", host, db_name, user, password)

if __name__ == "__main__":
    filename = sys.argv[1]
    project = sys.argv[2]
    startup_project = sys.argv[3]
    print(filename)
    print(project)
    print(startup_project)
    if not os.path.exists(filename):
        print("Error: %s does not exist" % filename)
        sys.exit(1)
    if not os.path.exists(project):
        print("Error: %s does not exist" % project)
        sys.exit(1)
    if not os.path.exists(startup_project):
        print("Error: %s does not exist" % startup_project)
        sys.exit(1)
    data = read_appsettings(filename)
    host, user, password = get_mysql_details(data)
    chain_db_names, fiat_db_names = get_db_names(data)
    init_dbs(project, startup_project, host, user, password, chain_db_names, fiat_db_names)
