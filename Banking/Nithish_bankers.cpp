#include <bits/stdc++.h>
#include <fstream>
#include <thread>
#include <mutex>
#include <chrono>
using namespace std;
mutex mtx;
struct Transaction {
    string type;
    double amount;
    string date;
};
class Account {
private:
    int accountNumber;
    string ownerName;
    string password;
    double balance;
    vector<Transaction> history;
public:
    Account() {}
    Account(int accNo, string name, string pass, double initialBalance = 0.0) {
        accountNumber = accNo;
        ownerName = name;
        password = pass;
        balance = initialBalance;
    }
    int getAccountNumber() const { return accountNumber; }
    string getOwnerName() const { return ownerName; }
    string getPassword() const { return password; }
    double getBalance() const { return balance; }
    void deposit(double amount) {
        if(amount <= 0) throw runtime_error("Deposit amount must be positive");
        balance += amount;
        addTransaction("Deposit", amount);
    }
    void withdraw(double amount) {
        if(amount <= 0) throw runtime_error("Withdraw amount must be positive");
        if (amount > balance) throw runtime_error("Not enough balance!");
        balance -= amount;
        addTransaction("Withdraw", amount);
    }
    void transfer(Account &to, double amount) {
        if(amount <= 0) throw runtime_error("Transfer amount must be positive");
        if (amount > balance) throw runtime_error("Not enough balance for transfer!");
        balance -= amount;
        to.balance += amount;
        addTransaction("Transfer to " + to.getOwnerName(), amount);
        to.addTransaction("Transfer from " + ownerName, amount);
    }
    void addTransaction(string type, double amount) {
        time_t now = time(0);
        string dt = ctime(&now);
        dt.pop_back();
        history.push_back({type, amount, dt});
    }
    void printStatement() {
        cout << "\n--- Statement for Account " << accountNumber << " (" << ownerName << ") ---\n";
        for (auto &t : history) {
            cout << t.date << " | " << t.type << " | " << t.amount << "\n";
        }
        cout << "Current Balance: " << balance << "\n";
    }
    void saveToFile(ofstream &ofs) {
        ofs.write((char*)&accountNumber, sizeof(accountNumber));
        size_t len = ownerName.size();
        ofs.write((char*)&len, sizeof(len));
        ofs.write(ownerName.c_str(), len);
        len = password.size();
        ofs.write((char*)&len, sizeof(len));
        ofs.write(password.c_str(), len);
        ofs.write((char*)&balance, sizeof(balance));
        size_t hist_size = history.size();
        ofs.write((char*)&hist_size, sizeof(hist_size));
        for(auto &t : history){
            len = t.type.size();
            ofs.write((char*)&len, sizeof(len));
            ofs.write(t.type.c_str(), len);
            ofs.write((char*)&t.amount, sizeof(t.amount));
            len = t.date.size();
            ofs.write((char*)&len, sizeof(len));
            ofs.write(t.date.c_str(), len);
        }
    }
    void loadFromFile(ifstream &ifs) {
        ifs.read((char*)&accountNumber, sizeof(accountNumber));
        size_t len;
        ifs.read((char*)&len, sizeof(len));
        ownerName.resize(len);
        ifs.read(&ownerName[0], len);
        ifs.read((char*)&len, sizeof(len));
        password.resize(len);
        ifs.read(&password[0], len);
        ifs.read((char*)&balance, sizeof(balance));
        size_t hist_size;
        ifs.read((char*)&hist_size, sizeof(hist_size));
        history.clear();
        for(size_t i=0;i<hist_size;i++){
            Transaction t;
            ifs.read((char*)&len, sizeof(len));
            t.type.resize(len);
            ifs.read(&t.type[0], len);
            ifs.read((char*)&t.amount, sizeof(t.amount));
            ifs.read((char*)&len, sizeof(len));
            t.date.resize(len);
            ifs.read(&t.date[0], len);
            history.push_back(t);
        }
    }
};
map<int, Account> accounts;
const string DATA_FILE = "accounts.dat";
template<typename T>
void logMessage(T msg) {
    lock_guard<mutex> lock(mtx);
    ofstream logFile("log.txt", ios::app);
    logFile << msg << endl;
}
void loadAccounts() {
    ifstream ifs(DATA_FILE, ios::binary);
    if(!ifs) return;
    while(ifs.peek()!=EOF){
        Account acc;
        acc.loadFromFile(ifs);
        accounts[acc.getAccountNumber()] = acc;
    }
}
void saveAccounts() {
    ofstream ofs(DATA_FILE, ios::binary | ios::trunc);
    for(auto &p : accounts){
        p.second.saveToFile(ofs);
    }
}
void backgroundLogger() {
    while(true){
        logMessage("Background logger running...");
        this_thread::sleep_for(chrono::seconds(5));
    }
}
int main(){
    loadAccounts();
    thread logger(backgroundLogger);
    logger.detach();
    int choice;
    cout << "Welcome to Nithishwarwar-Bankers\n";
    while(true){
        cout << "\n1. Admin Login\n2. Customer Login\n3. Exit\nChoice: ";
        cin >> choice;
        try{
            if(choice==1){
                string adminPass;
                cout << "Enter admin password (default 1234): ";
                cin >> adminPass;
                if(adminPass!="1234") throw runtime_error("Wrong admin password");
                int aChoice;
                while(true){
                    cout << "\nAdmin Menu:\n1. Create Account\n2. View All Accounts\n3. Logout\nChoice: ";
                    cin >> aChoice;
                    if(aChoice==1){
                        int accNo; string name, pass;
                        double balance;
                        cout << "Account Number: "; cin >> accNo;
                        if(accounts.find(accNo)!=accounts.end()) throw runtime_error("Account already exists");
                        cout << "Owner Name: "; cin.ignore(); getline(cin,name);
                        cout << "Password: "; cin >> pass;
                        cout << "Initial Balance: "; cin >> balance;
                        accounts[accNo] = Account(accNo,name,pass,balance);
                        saveAccounts();
                        cout << "Account created!\n";
                    }
                    else if(aChoice==2){
                        for(auto &p : accounts){
                            cout << p.first << " | " << p.second.getOwnerName() << " | Balance: " << p.second.getBalance() << "\n";
                        }
                    }
                    else break;
                }
            }
            else if(choice==2){
                int accNo; string pass;
                cout << "Account Number: "; cin >> accNo;
                if(accounts.find(accNo)==accounts.end()) throw runtime_error("Account not found");
                cout << "Password: "; cin >> pass;
                if(accounts[accNo].getPassword()!=pass) throw runtime_error("Wrong password");
                int cChoice;
                while(true){
                    cout << "\nCustomer Menu:\n1. Deposit\n2. Withdraw\n3. Transfer\n4. Statement\n5. Logout\nChoice: ";
                    cin >> cChoice;
                    if(cChoice==1){
                        double amt; cout << "Amount: "; cin >> amt;
                        accounts[accNo].deposit(amt);
                        saveAccounts();
                    }
                    else if(cChoice==2){
                        double amt; cout << "Amount: "; cin >> amt;
                        accounts[accNo].withdraw(amt);
                        saveAccounts();
                    }
                    else if(cChoice==3){
                        int toAcc; double amt;
                        cout << "Transfer To Account Number: "; cin >> toAcc;
                        if(accounts.find(toAcc)==accounts.end()) throw runtime_error("Target account not found");
                        cout << "Amount: "; cin >> amt;
                        accounts[accNo].transfer(accounts[toAcc],amt);
                        saveAccounts();
                    }
                    else if(cChoice==4){
                        accounts[accNo].printStatement();
                    }
                    else break;
                }
            }
            else break;
        }catch(exception &e){
            cout << "Error: " << e.what() << "\n";
        }
    }
    cout << "Thank you for Banking with Nithishwar-bankers .\n";
    return 0;
}
