$(function () {
    window.PDetails = null;
    var PeriodicalInfo = JSON.parse(sessionStorage.mPeriodicalInfo);
    console.info(PeriodicalInfo);
    avalon.ready(function () {
        window.PDetails = avalon.define({
            $id: 'details-box',
            deviceImg: getUserImG,
            number: 0,
            model: PeriodicalInfo
        });
        $('.modal-add .modal-body').mCustomScrollbar({
            theme: 'dark-3'
        });
        avalon.scan(document.body);
    });
});