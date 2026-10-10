// Disposable P01B construction gate. Reuses immutable P01 diagnostic utilities.
// No target is resumed by this first gate; public APIs only.
#define wmain preservedP01Main
#include "p01.cpp"
#undef wmain
#pragma comment(lib,"user32.lib")

DWORD restricted(HANDLE t,const S& label,PSID expected=nullptr){
 auto r=ti(t,TokenRestrictedSids);auto g=(TOKEN_GROUPS*)r.data();
 std::wcout<<L"RESTRICT_COUNT "<<label<<L" "<<g->GroupCount<<L"\n";
 bool found=false;
 for(DWORD i=0;i<g->GroupCount;i++){
  std::wcout<<L"RESTRICT_SID "<<label<<L" "<<sidstr(g->Groups[i].Sid)<<L" flags="<<g->Groups[i].Attributes<<L"\n";
  if(expected&&EqualSid(expected,g->Groups[i].Sid))found=true;
 }
 if(expected)std::wcout<<L"EXECUTION_SID_PRESENT "<<label<<L" "<<found<<L"\n";
 return expected?(found?1:0):g->GroupCount;
}
HANDLE boundedjob(SIZE_T memory,DWORD processes){
 HANDLE j=makejob();JOBOBJECT_EXTENDED_LIMIT_INFORMATION x{};
 x.BasicLimitInformation.LimitFlags=JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE|JOB_OBJECT_LIMIT_ACTIVE_PROCESS|JOB_OBJECT_LIMIT_JOB_MEMORY;
 x.BasicLimitInformation.ActiveProcessLimit=processes;x.JobMemoryLimit=memory;
 check(SetInformationJobObject(j,JobObjectExtendedLimitInformation,&x,sizeof(x)),"bounded job");return j;
}
PROCESS_INFORMATION construct(const S&exe,const S&cwd,const S&desktop,HANDLE t,PSID package,HANDLE outer,HANDLE candidate){
 STARTUPINFOEXW si{};si.StartupInfo.cb=sizeof(si);si.StartupInfo.lpDesktop=(LPWSTR)desktop.c_str();
 SIZE_T n=0;InitializeProcThreadAttributeList(nullptr,3,0,&n);std::vector<BYTE>b(n);si.lpAttributeList=(LPPROC_THREAD_ATTRIBUTE_LIST)b.data();
 check(InitializeProcThreadAttributeList(si.lpAttributeList,3,0,&n),"attributes");
 HANDLE jobs[]={outer,candidate};SECURITY_CAPABILITIES caps{};caps.AppContainerSid=package;
 DWORD policy=PROCESS_CREATION_ALL_APPLICATION_PACKAGES_OPT_OUT;
 try{
  check(UpdateProcThreadAttribute(si.lpAttributeList,0,PROC_THREAD_ATTRIBUTE_JOB_LIST,jobs,sizeof jobs,nullptr,nullptr),"creation jobs");
  check(UpdateProcThreadAttribute(si.lpAttributeList,0,PROC_THREAD_ATTRIBUTE_SECURITY_CAPABILITIES,&caps,sizeof caps,nullptr,nullptr),"package");
  check(UpdateProcThreadAttribute(si.lpAttributeList,0,PROC_THREAD_ATTRIBUTE_ALL_APPLICATION_PACKAGES_POLICY,&policy,sizeof policy,nullptr,nullptr),"LPAC");
  S cmd=L"\""+exe+L"\" never-release";S env=L"SystemRoot=C:\\Windows";env+=wchar_t(0);env+=L"TEMP="+cwd;env+=wchar_t(0);env+=wchar_t(0);
  PROCESS_INFORMATION p{};BOOL ok=CreateProcessAsUserW(t,exe.c_str(),cmd.data(),nullptr,nullptr,FALSE,
    CREATE_SUSPENDED|CREATE_NO_WINDOW|CREATE_UNICODE_ENVIRONMENT|EXTENDED_STARTUPINFO_PRESENT,env.data(),cwd.c_str(),&si.StartupInfo,&p);
  DWORD e=ok?0:GetLastError();std::wcout<<L"CREATE_FINAL ok="<<ok<<L" error="<<e<<L"\n";
  if(!ok){SetLastError(e);check(false,"public composition unavailable");}DeleteProcThreadAttributeList(si.lpAttributeList);return p;
 }catch(...){DeleteProcThreadAttributeList(si.lpAttributeList);throw;}
}
int wmain(int argc,wchar_t**argv){
 if(argc!=3)return 2;S root=argv[1],nonce=argv[2],profile=L"GagambaAR7B"+nonce;
 HANDLE caller=nullptr,rt=nullptr,outer=nullptr,candidate=nullptr;PSID package=nullptr,execution=nullptr;
 HWINSTA station=nullptr;HDESK desk=nullptr;PROCESS_INFORMATION p{};S folder,staged;int status=0;bool rootMade=false;
 try{
  check(OpenProcessToken(GetCurrentProcess(),TOKEN_QUERY|TOKEN_DUPLICATE|TOKEN_ASSIGN_PRIMARY,&caller),"caller token");
  token(GetCurrentProcess(),L"caller");auto e=ti(caller,TokenElevation);check(!((TOKEN_ELEVATION*)e.data())->TokenIsElevated,"non-elevated required");
  DWORD prior=restricted(caller,L"caller");check(prior==0,"existing restricting list prerequisite not satisfied");
  GUID guid;check(SUCCEEDED(CoCreateGuid(&guid)),"nonce SID guid");DWORD* words=(DWORD*)&guid;
  S es=L"S-1-5-21-"+std::to_wstring(words[0])+L"-"+std::to_wstring(words[1])+L"-"+std::to_wstring(words[2])+L"-"+std::to_wstring(words[3]);
  check(ConvertStringSidToSidW(es.c_str(),&execution),"execution SID");SID_AND_ATTRIBUTES sa{execution,0};
  check(CreateRestrictedToken(caller,DISABLE_MAX_PRIVILEGE,0,nullptr,0,nullptr,1,&sa,&rt),"full restricted token");
  std::wcout<<L"FLAGS DISABLE_MAX_PRIVILEGE=1 WRITE_RESTRICTED=0 SANDBOX_INERT=0\n";
  check(restricted(rt,L"input",execution)==1,"input fresh SID missing");
  check(SUCCEEDED(CreateAppContainerProfile(profile.c_str(),profile.c_str(),L"Disposable AR7 P01B",nullptr,0,&package)),"profile");
  check(!EqualSid(package,execution),"distinct execution/package SID");LPWSTR pp=nullptr;
  check(SUCCEEDED(GetAppContainerFolderPath(sidstr(package).c_str(),&pp)),"profile path");folder=pp;CoTaskMemFree(pp);
  std::wcout<<L"PROFILE "<<profile<<L" "<<sidstr(package)<<L" "<<folder<<L"\n";
  auto u=ti(caller,TokenUser);S owner=sidstr(((TOKEN_USER*)u.data())->User.Sid);
  S grant=L"(A;;GRGX;;;"+sidstr(package)+L")(A;;GRGX;;;"+es+L")";
  check(CreateDirectoryW(root.c_str(),nullptr),"owned root");rootMade=true;acl(root,owner,grant,false);
  WCHAR exe[32768];check(GetModuleFileNameW(nullptr,exe,32768)>0,"module path");staged=root+L"\\fixture.exe";
  check(CopyFileW(exe,staged.c_str(),TRUE),"stage");acl(staged,owner,grant,false);
  // Private noninteractive window station/desktop, never grants on existing host objects.
  S security=L"D:P(A;;GA;;;"+owner+L")(A;;GA;;;SY)(A;;GA;;;"+sidstr(package)+L")(A;;GA;;;"+es+L")S:(ML;;NW;;;LW)";
  PSECURITY_DESCRIPTOR sd=nullptr;check(ConvertStringSecurityDescriptorToSecurityDescriptorW(security.c_str(),1,&sd,nullptr),"desktop ACL");
  SECURITY_ATTRIBUTES attr{sizeof(attr),sd,FALSE};S wn=L"AR7B"+nonce;
  station=CreateWindowStationW(wn.c_str(),0,WINSTA_ALL_ACCESS,&attr);check(station!=nullptr,"owned station");
  HWINSTA original=GetProcessWindowStation();check(SetProcessWindowStation(station),"select owned station");
  desk=CreateDesktopW(L"Probe",nullptr,nullptr,0,DESKTOP_CREATEWINDOW|DESKTOP_READOBJECTS|DESKTOP_WRITEOBJECTS|DELETE|READ_CONTROL|WRITE_DAC|WRITE_OWNER,&attr);
  DWORD de=desk?0:GetLastError();check(SetProcessWindowStation(original),"restore station");LocalFree(sd);if(!desk){SetLastError(de);check(false,"owned desktop");}
  std::wcout<<L"DESKTOP "<<wn<<L"\\Probe owned_only=1\n";
  outer=boundedjob(256*1024*1024,8);candidate=boundedjob(64*1024*1024,4);
  p=construct(staged,root,wn+L"\\Probe",rt,package,outer,candidate);
  std::wcout<<L"SUSPENDED pid="<<p.dwProcessId<<L" targetInstructionsReleased=0\n";
  token(p.hProcess,L"final");HANDLE ft;check(OpenProcessToken(p.hProcess,TOKEN_QUERY,&ft),"final token");
  DWORD retained=restricted(ft,L"final",execution);CloseHandle(ft);BOOL inO=FALSE,inC=FALSE;
  check(IsProcessInJob(p.hProcess,outer,&inO)&&IsProcessInJob(p.hProcess,candidate,&inC),"job oracle");
  std::wcout<<L"JOBS outer="<<inO<<L" candidate="<<inC<<L"\n";
  std::wcout<<(retained?L"CONSTRUCTION_GATE RETAINED - useful-startup/authority tests pending\n":L"CONSTRUCTION_GATE BLOCKED - restricting SID lost before release\n");
 }catch(const std::exception&e){std::cerr<<"GATE_ERROR "<<e.what()<<"\n";status=1;}
 if(outer){check(TerminateJobObject(outer,125),"outer stop");ULONGLONG until=GetTickCount64()+3000;while(active(outer)&&GetTickCount64()<until)Sleep(10);std::wcout<<L"CLEANUP outerActive="<<active(outer)<<L"\n";if(active(outer))return 3;}
 if(p.hProcess){WaitForSingleObject(p.hProcess,2000);CloseHandle(p.hThread);CloseHandle(p.hProcess);}
 if(candidate)CloseHandle(candidate);if(outer)CloseHandle(outer);if(rt)CloseHandle(rt);if(caller)CloseHandle(caller);
 if(desk)check(CloseDesktop(desk),"desktop close");if(station)check(CloseWindowStation(station),"station close");
 if(package){HRESULT hr=DeleteAppContainerProfile(profile.c_str());bool absent=GetFileAttributesW(folder.c_str())==INVALID_FILE_ATTRIBUTES;
  std::wcout<<L"CLEANUP profileDelete="<<std::hex<<hr<<std::dec<<L" folderAbsent="<<absent<<L"\n";if(FAILED(hr)||!absent)status=3;
  S key=L"Software\\Classes\\Local Settings\\Software\\Microsoft\\Windows\\CurrentVersion\\AppContainer\\Mappings\\"+sidstr(package);HKEY k;LSTATUS st=RegOpenKeyExW(HKEY_CURRENT_USER,key.c_str(),0,KEY_READ,&k);if(st==0)RegCloseKey(k);
  std::wcout<<L"CLEANUP mappingStatus="<<st<<L"\n";if(st!=ERROR_FILE_NOT_FOUND)status=3;FreeSid(package);}
 if(execution)LocalFree(execution);
 if(!staged.empty()&&!DeleteFileW(staged.c_str())&&GetLastError()!=ERROR_FILE_NOT_FOUND)status=3;
 if(rootMade&&!RemoveDirectoryW(root.c_str()))status=3;
 std::wcout<<L"CLEANUP result="<<status<<L" rootAbsent="<<(GetFileAttributesW(root.c_str())==INVALID_FILE_ATTRIBUTES)<<L"\n";return status;
}
