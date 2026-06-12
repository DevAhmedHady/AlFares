import{Component,computed,inject,signal}from'@angular/core';import{NavigationEnd,Router,RouterLink,RouterLinkActive,RouterOutlet}from'@angular/router';import{ButtonModule}from'primeng/button';import{ToastModule}from'primeng/toast';import{AuthStore}from'../core/auth/auth.store';import{AuthService}from'../core/auth/auth.service';import{TaskNotificationService}from'../core/task-notification.service';
interface NavItem{path?:string;label:string;icon:string;permission?:string;children?:NavItem[];}
@Component({selector:'app-shell',standalone:true,imports:[RouterOutlet,RouterLink,RouterLinkActive,ButtonModule,ToastModule],templateUrl:'./shell.html',styleUrl:'./shell.scss'})
export class ShellComponent{private readonly store=inject(AuthStore);private readonly auth=inject(AuthService);readonly email=this.store.email;private readonly items:NavItem[]=[
  {path:'/dashboard',label:'لوحة المعلومات',icon:'pi pi-chart-pie',permission:'dashboard.charts.read'},
  {path:'/clients',label:'العملاء',icon:'pi pi-users',permission:'clients.read'},
  {path:'/expenses',label:'المصروفات',icon:'pi pi-wallet',permission:'expenses.read',children:[{path:'/expenses/types',label:'أنواع المصروفات',icon:'pi pi-tags',permission:'expenses.read'}]},
  {path:'/revenues',label:'الإيرادات',icon:'pi pi-money-bill',permission:'revenues.read',children:[{path:'/revenues/types',label:'أنواع الإيرادات',icon:'pi pi-tags',permission:'revenues.read'}]},
  {path:'/cars',label:'السيارات',icon:'pi pi-car',permission:'cars.read'},
  {path:'/workers',label:'العمال',icon:'pi pi-briefcase',permission:'workers.read'},
  {label:'التقارير',icon:'pi pi-file',children:[
    {path:'/reports/owners',label:'تقارير الحسابات',icon:'pi pi-file',permission:'reports.read'},
    {path:'/reports/workers',label:'تقارير العمال',icon:'pi pi-file-excel',permission:'workers.read'},
  ]},
  {path:'/todos',label:'المهام',icon:'pi pi-check-square',permission:'todos.read'},
  {path:'/users',label:'المستخدمون',icon:'pi pi-id-card',permission:'identity.users.read'},
];
  readonly nav=computed(()=>this.items
    .map(i=>({...i,children:i.children?.filter(c=>this.store.has(c.permission))}))
    .filter(i=>(i.permission?this.store.has(i.permission):true)&&(!i.children||i.children.length>0||i.path)));

  private readonly STORAGE_KEY='nav-expanded';
  private readonly router=inject(Router);
  readonly expanded=signal<Set<string>>(this.loadExpanded());

  constructor(){
    inject(TaskNotificationService).start();
    this.expandActiveGroup(this.router.url);
    this.router.events.subscribe(e=>{ if(e instanceof NavigationEnd) this.expandActiveGroup(e.urlAfterRedirects); });
  }

  private loadExpanded():Set<string>{
    try{ return new Set(JSON.parse(localStorage.getItem(this.STORAGE_KEY)??'[]')); }
    catch{ return new Set(); }
  }

  private saveExpanded():void{
    localStorage.setItem(this.STORAGE_KEY, JSON.stringify([...this.expanded()]));
  }

  private expandActiveGroup(url:string):void{
    for(const item of this.items){
      if(item.children?.some(c=>c.path && url.startsWith(c.path))){
        this.expanded.update(set=>{ const next=new Set(set); next.add(item.label); return next; });
      }
    }
  }

  isExpanded(label:string):boolean{ return this.expanded().has(label); }

  toggle(label:string):void{
    this.expanded.update(set=>{
      const next=new Set(set);
      if(next.has(label)) next.delete(label); else next.add(label);
      return next;
    });
    this.saveExpanded();
  }

  logout(){this.auth.logout();location.assign('/login');}
}
