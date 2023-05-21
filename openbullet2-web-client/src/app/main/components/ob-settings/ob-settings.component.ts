import { Component, HostListener, OnInit } from '@angular/core';
import { SettingsService } from '../../services/settings.service';
import { OBSettingsDto, ProxyCheckTarget } from '../../dtos/settings/ob-settings.dto';
import { ConfirmationService, MessageService } from 'primeng/api';
import { FieldValidity } from 'src/app/shared/utils/forms';
import { faPen, faPlus, faX } from '@fortawesome/free-solid-svg-icons';

@Component({
  selector: 'app-ob-settings',
  templateUrl: './ob-settings.component.html',
  styleUrls: ['./ob-settings.component.scss']
})
export class OBSettingsComponent implements OnInit {
  // TODO: Add a guard as well so if the user navigates away
  // from the page using the router it will also prompt the warning!
  @HostListener('window:beforeunload') confirmLeavingWithoutSaving(): boolean {
    return !this.touched;
  }

  faPlus = faPlus;
  faX = faX;
  faPen = faPen;
  createProxyCheckTargetModalVisible: boolean = false;
  updateProxyCheckTargetModalVisible: boolean = false;
  fieldsValidity: { [key: string] : boolean; } = {};
  settings: OBSettingsDto | null = null;
  touched: boolean = false;
  configSections: string[] = [
    'metadata',
    'readme',
    'stacker',
    'loliCode',
    'settings',
    'cSharpCode',
    'loliScript'
  ];
  jobDisplayModes: string[] = [
    'standard',
    'detailed'
  ];
  selectedProxyCheckTarget: ProxyCheckTarget | null = null;

  constructor(private settingsService: SettingsService,
    private confirmationService: ConfirmationService,
    private messageService: MessageService) {}
  
  ngOnInit(): void {
    this.getSettings();
  }

  getSettings() {
    this.settingsService.getSettings()
      .subscribe(settings => this.settings = settings);
  }

  saveSettings() {
    if (this.settings === null) return;
    this.settingsService.updateSettings(this.settings)
      .subscribe(settings => {
        this.messageService.add({
          severity: 'success',
          summary: 'Saved',
          detail: 'The settings were successfully saved'
        });
        this.touched = false;
        this.settings = settings;
      });
  }

  confirmRestoreDefaults() {
    this.confirmationService.confirm({
      message: `You are about to restore the default settings. 
      Are you sure that you want to proceed?`,
      header: 'Confirmation',
      icon: 'pi pi-exclamation-triangle',
      accept: () => this.restoreDefaults()
    });
  }

  restoreDefaults() {
    this.settings = null;
    this.settingsService.getDefaultSettings()
      .subscribe(defaultSettings => {
        this.settingsService.updateSettings(defaultSettings)
          .subscribe(settings => {
            this.messageService.add({
              severity: 'success',
              summary: 'Restored',
              detail: 'Settings restored to the default values'
            });
            this.settings = settings;
          })
      });
  }

  onValidityChange(validity: FieldValidity) {
    this.fieldsValidity[validity.key] = validity.valid;
  }

  // Can save if touched and every field is valid
  canSave() {
    return this.touched && Object.values(this.fieldsValidity).every(v => v);
  }

  openCreateProxyCheckTargetModal() {
    this.createProxyCheckTargetModalVisible = true;
  }

  openUpdateProxyCheckTargetModal(target: ProxyCheckTarget) {
    this.selectedProxyCheckTarget = target;
    this.updateProxyCheckTargetModalVisible = true;
  }

  createProxyCheckTarget(target: ProxyCheckTarget) {
    this.settings!.generalSettings.proxyCheckTargets.push(target);
    this.touched = true;
    this.createProxyCheckTargetModalVisible = false;
  }

  updateProxyCheckTarget(target: ProxyCheckTarget) {
    if (this.selectedProxyCheckTarget === null) return;
    this.selectedProxyCheckTarget.url = target.url;
    this.selectedProxyCheckTarget.successKey = target.successKey;
    this.touched = true;
    this.updateProxyCheckTargetModalVisible = false;
  }

  deleteProxyCheckTarget(target: ProxyCheckTarget) {
    const index = this.settings!.generalSettings.proxyCheckTargets.indexOf(target);
    if (index !== -1) {
      this.settings!.generalSettings.proxyCheckTargets.splice(index, 1);
      this.touched = true;
    }
  }
}
