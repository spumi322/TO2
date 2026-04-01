import { Component, OnInit, OnDestroy } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { Format } from '../../../models/tournament';
import { TournamentService } from '../../../services/tournament/tournament.service';
import { TournamentConfigService } from '../../../services/tournament/tournament-config.service';
import { Router } from '@angular/router';
import { Subject, takeUntil } from 'rxjs';
import { finalize } from 'rxjs/operators';
import { MessageService } from 'primeng/api';

@Component({
  selector: 'app-create-tournament',
  templateUrl: './create-tournament.component.html',
  styleUrls: ['./create-tournament.component.css']
})
export class CreateTournamentComponent implements OnInit, OnDestroy {
  form!: FormGroup;
  Format = Format;
  isSubmitting = false;
  private destroy$ = new Subject<void>();

  constructor(
    private fb: FormBuilder,
    private tournamentService: TournamentService,
    private configService: TournamentConfigService,
    private router: Router,
    private messageService: MessageService
  ) {}

  ngOnInit(): void {
    this.form = this.fb.group({
      name: ['', [Validators.required, Validators.minLength(4), Validators.maxLength(60)]],
      description: ['', [Validators.maxLength(250)]],
      format: [null, Validators.required],
      maxTeams: [null],
      numberOfGroups: [null],
      advancingPerGroup: [null],
    });

    this.form.get('format')!.valueChanges
      .pipe(takeUntil(this.destroy$))
      .subscribe(() => this.onFormatChange());

    this.form.get('maxTeams')!.valueChanges
      .pipe(takeUntil(this.destroy$))
      .subscribe(() => this.onMaxTeamsChange());

    this.form.get('numberOfGroups')!.valueChanges
      .pipe(takeUntil(this.destroy$))
      .subscribe(() => this.onNumberOfGroupsChange());
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  get format(): Format | null {
    return this.form.get('format')?.value ?? null;
  }

  get maxTeams(): number {
    return +(this.form.get('maxTeams')?.value) || 0;
  }

  get numberOfGroups(): number {
    return +(this.form.get('numberOfGroups')?.value) || 0;
  }

  get advancingPerGroup(): number {
    return +(this.form.get('advancingPerGroup')?.value) || 0;
  }

  get numberOfGroupsOptions(): number[] {
    if (!this.maxTeams || this.maxTeams < 3) return [];
    const max = Math.floor(this.maxTeams / 3);
    return Array.from({ length: max }, (_, i) => i + 1);
  }

  get advancingPerGroupOptions(): number[] {
    if (!this.maxTeams || !this.numberOfGroups) return [];
    const minGroupSize = Math.floor(this.maxTeams / this.numberOfGroups);
    if (minGroupSize < 3) return [];
    const min = this.numberOfGroups === 1 ? 2 : 1;
    return Array.from({ length: minGroupSize - min }, (_, i) => i + min);
  }

  get groupSizes(): number[] {
    if (!this.maxTeams || !this.numberOfGroups) return [];
    return this.configService.distributeTeams(this.maxTeams, this.numberOfGroups);
  }

  get bracketSize(): number {
    if (!this.format) return 0;
    return this.configService.computeBracketSize(
      this.format,
      this.maxTeams,
      this.numberOfGroups || undefined,
      this.advancingPerGroup || undefined
    );
  }

  get advancingTeamCount(): number {
    if (this.format === Format.BracketOnly) return this.maxTeams;
    return this.numberOfGroups * this.advancingPerGroup;
  }

  get byeCount(): number {
    if (!this.bracketSize) return 0;
    return this.configService.computeByeCount(this.bracketSize, this.advancingTeamCount);
  }

  readonly MATCH_GAP = 0.5;  // rem gap between matches
  private get SLOT_H_CALC(): number { return 2; }
  get slotH(): number { return this.SLOT_H_CALC - this.MATCH_GAP / 2; } // 1.75rem

  get maxTeamsInRange(): boolean {
    return this.maxTeams >= 4 && this.maxTeams <= 32;
  }

  get groupPreviewReady(): boolean {
    return this.maxTeamsInRange
      && this.numberOfGroups >= 1
      && Math.floor(this.maxTeams / this.numberOfGroups) >= 3;
  }

  get bracketPreviewReady(): boolean {
    if (!this.maxTeamsInRange) return false;
    if (this.format === Format.BracketOnly) return true;
    if (this.format === Format.GroupsAndBracket) {
      return this.groupPreviewReady && this.advancingPerGroup >= 1;
    }
    return false;
  }

  // ── Bracket rounds for visual tree ──────────────────────────────────────

  get bracketRounds(): Array<{
    matches: Array<{ top: string; bottom: string; topBye: boolean; bottomBye: boolean; placeholder: boolean }>;
    connectorHeight: number;
    matchMargin: number;
    isLast: boolean;
  }> {
    if (!this.bracketPreviewReady || !this.bracketSize) return [];

    const totalRounds = Math.log2(this.bracketSize);
    const firstRound = this.configService.generateMatchups(this.bracketSize);
    const adv = this.advancingTeamCount;
    const result = [];

    for (let r = 0; r < totalRounds; r++) {
      const matchCount = this.bracketSize >> (r + 1);
      const connectorHeight = this.SLOT_H_CALC * Math.pow(2, r);
      const matchMargin = this.SLOT_H_CALC * (Math.pow(2, r) - 1) + this.MATCH_GAP / 2;
      const isLast = r === totalRounds - 1;

      const matches = r === 0
        ? firstRound.map(([a, b]) => ({
            top: a > adv ? 'BYE' : `#${a}`,
            bottom: b > adv ? 'BYE' : `#${b}`,
            topBye: a > adv,
            bottomBye: b > adv,
            placeholder: false
          }))
        : Array(matchCount).fill(null).map(() => ({
            top: '', bottom: '', topBye: false, bottomBye: false, placeholder: true
          }));

      result.push({ matches, connectorHeight, matchMargin, isLast });
    }

    return result;
  }

  get bracketTreeHeight(): number {
    return this.bracketSize * this.SLOT_H_CALC;
  }

  get hasUnequalGroups(): boolean {
    const sizes = this.groupSizes;
    return sizes.length > 0 && new Set(sizes).size > 1;
  }

  get showGroupPreview(): boolean {
    return (this.format === Format.GroupsOnly || this.format === Format.GroupsAndBracket)
      && this.groupSizes.length > 0;
  }

  get showBracketPreview(): boolean {
    return (this.format === Format.BracketOnly || this.format === Format.GroupsAndBracket)
      && this.bracketSize > 0;
  }

  get isFormValid(): boolean {
    const fmt = this.format;
    if (!fmt || !this.form.get('name')?.valid) return false;
    if (!this.maxTeams || this.maxTeams < 4 || this.maxTeams > 32) return false;
    if (fmt === Format.GroupsOnly || fmt === Format.GroupsAndBracket) {
      if (!this.numberOfGroups || Math.floor(this.maxTeams / this.numberOfGroups) < 3) return false;
    }
    if (fmt === Format.GroupsAndBracket) {
      if (!this.advancingPerGroup) return false;
      const minGroupSize = Math.floor(this.maxTeams / this.numberOfGroups);
      if (this.advancingPerGroup >= minGroupSize) return false;
    }
    return true;
  }

  selectFormat(format: Format): void {
    this.form.get('format')!.setValue(format);
  }

  private onFormatChange(): void {
    this.form.patchValue({ maxTeams: null, numberOfGroups: null, advancingPerGroup: null }, { emitEvent: false });
    this.updateValidators();
  }

  private onMaxTeamsChange(): void {
    if (this.numberOfGroups && this.maxTeams && Math.floor(this.maxTeams / this.numberOfGroups) < 3) {
      this.form.patchValue({ numberOfGroups: null, advancingPerGroup: null }, { emitEvent: false });
    }
    this.updateValidators();
  }

  private onNumberOfGroupsChange(): void {
    if (this.advancingPerGroup && this.numberOfGroups && this.maxTeams) {
      const minGroupSize = Math.floor(this.maxTeams / this.numberOfGroups);
      const minAdvancing = this.numberOfGroups === 1 ? 2 : 1;
      if (this.advancingPerGroup >= minGroupSize || this.advancingPerGroup < minAdvancing) {
        this.form.patchValue({ advancingPerGroup: null }, { emitEvent: false });
      }
    }
  }

  private updateValidators(): void {
    const fmt = this.format;
    const maxTeamsCtrl = this.form.get('maxTeams')!;
    const groupsCtrl = this.form.get('numberOfGroups')!;
    const advCtrl = this.form.get('advancingPerGroup')!;

    maxTeamsCtrl.setValidators([Validators.required, Validators.min(4), Validators.max(32)]);

    if (fmt === Format.GroupsOnly || fmt === Format.GroupsAndBracket) {
      groupsCtrl.setValidators([Validators.required]);
    } else {
      groupsCtrl.clearValidators();
    }

    if (fmt === Format.GroupsAndBracket) {
      advCtrl.setValidators([Validators.required]);
    } else {
      advCtrl.clearValidators();
    }

    maxTeamsCtrl.updateValueAndValidity({ emitEvent: false });
    groupsCtrl.updateValueAndValidity({ emitEvent: false });
    advCtrl.updateValueAndValidity({ emitEvent: false });
  }

  submit(): void {
    if (!this.isFormValid || this.isSubmitting) return;

    this.isSubmitting = true;
    const request = this.configService.buildCreateRequest(this.form.value);

    this.tournamentService.createTournament(request)
      .pipe(finalize(() => this.isSubmitting = false))
      .subscribe({
        next: (res) => {
          this.messageService.add({ severity: 'success', summary: 'Success', detail: 'Tournament created!' });
          setTimeout(() => this.router.navigate(['/tournament', res.id]), 1500);
        },
        error: (err) => {
          this.messageService.add({
            severity: 'error',
            summary: 'Error',
            detail: err.error?.detail || err.error?.message || 'Failed to create tournament.'
          });
        }
      });
  }
}
