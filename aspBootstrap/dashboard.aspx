<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="dashboard.aspx.cs" Inherits="aspBootstrap.dashboard" MasterPageFile="~/master.Master"%>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="ContentPlaceHolder1" runat="server">
        <div id="wrapper">
            <!-- Page -->
            <div id="page-wrapper">
                <div class="row">
                    <div class="col-lg-12">
                        <h1 class="page-header">Dashboard</h1>
                    </div>
                    <!-- /.col-lg-12 -->
                </div>
                <!-- /.row -->
                <asp:ScriptManager ID="dashScriptManager" EnablePartialRendering="true" runat="server"></asp:ScriptManager>
                <asp:UpdatePanel ID="counterPanel" runat="server" UpdateMode="Conditional" OnLoad="counterPanelUpdate">
                    <ContentTemplate>
                        <asp:Timer ID="dashTimer" OnTick="dashTimerTick" runat="server" Interval="5000"></asp:Timer>
                        <div class="row">
                            <div class="col-lg-3 col-md-6">
                                <div class="panel panel-primary">
                                    <div class="panel-heading">
                                        <div class="row">
                                            <div class="col-xs-3">
                                                <i class="fa fa-support fa-5x"></i>
                                            </div>
                                            <div class="col-xs-9 text-right">
                                                <div class="huge">
                                                    <asp:Label runat="server" ID="lblPagesViewed"></asp:Label>
                                                </div>
                                                <div>Pages</div>
                                            </div>
                                        </div>
                                    </div>
                                    <a href="#">
                                        <div class="panel-footer">
                                            <span class="pull-left">View Details</span>
                                            <span class="pull-right"><i class="fa fa-arrow-circle-right"></i></span>
                                            <div class="clearfix"></div>
                                        </div>
                                    </a>
                                </div>
                            </div>
                            <div class="col-lg-3 col-md-6">
                                <div class="panel panel-green">
                                    <div class="panel-heading">
                                        <div class="row">
                                            <div class="col-xs-3">
                                                <i class="fa fa-support fa-5x"></i>
                                            </div>
                                            <div class="col-xs-9 text-right">
                                                <div class="huge">
                                                    <asp:Label runat="server" ID="lblProductCount"></asp:Label>
                                                </div>
                                                <div>Products</div>
                                            </div>
                                        </div>
                                    </div>
                                    <a href="#">
                                        <div class="panel-footer">
                                            <span class="pull-left">View Details</span>
                                            <span class="pull-right"><i class="fa fa-arrow-circle-right"></i></span>
                                            <div class="clearfix"></div>
                                        </div>
                                    </a>
                                </div>
                            </div>
                            <div class="col-lg-3 col-md-6">
                                <div class="panel panel-yellow">
                                    <div class="panel-heading">
                                        <div class="row">
                                            <div class="col-xs-3">
                                                <i class="fa fa-support fa-5x"></i>
                                            </div>
                                            <div class="col-xs-9 text-right">
                                                <div class="huge">
                                                    <asp:Label runat="server" ID="lblMissingImageCount"></asp:Label>
                                                </div>
                                                <div>Missing Images</div>
                                            </div>
                                        </div>
                                    </div>
                                    <a href="#">
                                        <div class="panel-footer">
                                            <span class="pull-left">View Details</span>
                                            <span class="pull-right"><i class="fa fa-arrow-circle-right"></i></span>
                                            <div class="clearfix"></div>
                                        </div>
                                    </a>
                                </div>
                            </div>
                            <div class="col-lg-3 col-md-6">
                                <div class="panel panel-red">
                                    <div class="panel-heading">
                                        <div class="row">
                                            <div class="col-xs-3">
                                                <i class="fa fa-support fa-5x"></i>
                                            </div>
                                            <div class="col-xs-9 text-right">
                                                <div class="huge">
                                                    <asp:Label runat="server" ID="lblElapsed"></asp:Label>
                                                </div>
                                                <div>Time Elapsed</div>
                                            </div>
                                        </div>
                                    </div>
                                    <a href="#">
                                        <div class="panel-footer">
                                            <span class="pull-left">View Details</span>
                                            <span class="pull-right"><i class="fa fa-arrow-circle-right"></i></span>
                                            <div class="clearfix"></div>
                                        </div>
                                    </a>
                                </div>
                            </div>
                        </div>
                    </ContentTemplate>
                </asp:UpdatePanel>
                <!-- /.row -->
                <div class="row">
                    <div class="col-lg-8">
                        <div class="panel panel-default">
                            <div class="panel-heading">
                                <i class="fa fa-bar-chart-o fa-fw"></i>
                                <asp:Label runat="server" Text="Crawl Graphs" ID="lblGraphName"></asp:Label>
                                <div class="pull-right">
                                    <div class="btn-group">
                                        <button type="button" class="btn btn-default btn-xs dropdown-toggle" data-toggle="dropdown">
                                            Actions
                                       
                                            <span class="caret"></span>
                                        </button>
                                        <ul class="dropdown-menu pull-right" role="menu">
                                            <li>
                                                <asp:LinkButton runat="server" ID="graphPagesLink" OnClick="graphPages" Text="Pages"></asp:LinkButton></li>
                                            <li>
                                                <asp:LinkButton runat="server" ID="graphProductsLink" OnClick="graphProducts" Text="Products"></asp:LinkButton></li>
                                            <li>
                                                <asp:LinkButton runat="server" ID="graphMissingImagesLink" OnClick="graphMissingImages" Text="Missing Images"></asp:LinkButton></li>
                                        </ul>
                                    </div>
                                </div>
                            </div>
                            <!-- /.panel-heading -->
                            <asp:UpdatePanel runat="server" ID="graphPanel" UpdateMode="Conditional">
                                <ContentTemplate>
                                    <div class="panel-body">
                                        <div id="morris-area-chart"></div>
                                    </div>
                                </ContentTemplate>
                            </asp:UpdatePanel>
                            <!-- /.panel-body -->
                        </div>
                        <!-- /.panel -->
                    </div>
                    <!-- /.col-lg-8 -->
                    <div class="col-lg-4">
                        <div class="panel panel-default">
                            <div class="panel-heading">
                                <i class="fa fa-bell fa-fw"></i>Notifications Panel
                           
                            </div>
                            <!-- /.panel-heading -->
                            <div class="panel-body">
                                <div class="list-group">
                                    <asp:DataGrid ID="dgNotifications" CssClass="table table-striped table-bordered table-hover" runat="server">
                                    </asp:DataGrid>
                                </div>
                                <!-- /.list-group -->
                                <a href="#" class="btn btn-default btn-block">View All Alerts</a>
                            </div>
                            <!-- /.panel-body -->
                        </div>
                    </div>
                    <!-- /.panel -->
                </div>
                <!-- /#page-wrapper -->
            </div>
            <!-- /#wrapper -->
        </div>



</asp:Content>
<asp:Content ID="Content3" ContentPlaceHolderID="footer" runat="server">
<!-- Scripts -->
        <!-- Morris Charts JavaScript -->
        <script src="js/plugins/morris/raphael.min.js"></script>
        <script src="js/plugins/morris/morris.min.js"></script>
        <!-- <script src="js/plugins/morris/morris-data.js"></script> //Moris test data-->

        <!-- Graphing -->
        <script type="text/javascript">
            function Graph() {
                Morris.Bar({
                    element: 'morris-area-chart',
                    data: graphData,
                    xkey: 'y',
                    ykeys: ['a'],
                    labels: ['Num', 'CrawlID']
                });
            }
        </script>

    <!-- /Scripts -->
</asp:Content>
